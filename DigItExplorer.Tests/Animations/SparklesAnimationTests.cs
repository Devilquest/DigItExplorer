using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins sparkle animation tables and frame hashes across color variants against reference files.</summary>
public class SparklesAnimationTests
{
    private static readonly string[] Names =
    [
        "sparkle_pink", "sparkle_orange", "sparkle_purple", "sparkle_green", "sparkle_gold",
        "sparkle_red", "sparkle_silver", "sparkle_blue", "sparkle_bronze",
    ];

    // One SHA-256 per cycle over its concatenated RGBA frames.
    private static readonly string[] Shas =
    [
        "A76920F11966C64E8646F79D0E76D02CE8838803D9DFFCB21DBE1C743FC59BC3", // sparkle_pink
        "5AA4D9A91D81E1A202E506B555850997AF0DD31D92F99CA3DCB3A24B7E8EB13F", // sparkle_orange
        "AED54020F8C81B9E9C7DC068F9A60BF61EFDF50853B62C062658304530E324B3", // sparkle_purple
        "64D05A28960DB06E3C45D6025C0D9A9470CB7034833F0F4B4461106F4A72C6EC", // sparkle_green
        "B7CB10BCA75BC1776D7B55DE03476E344569898C95DF31DC4D324D6E26038D83", // sparkle_gold
        "21AF0165BCFB3397C96D28546F66A9E778489185279160DB701CBAD8E0C38016", // sparkle_red
        "13A0FE75CD65F4813E2A6668B7AC7B9F46BA1820C41AA55A5A2EA46BA6545ACC", // sparkle_silver
        "A40D042199DD75B0E22F415682212188162501E70FCE07D9C3B65134CED57627", // sparkle_blue
        "F92A26FA067CB0C14A62B2A286E7B6AF38699E30B1656990AAB2A78B3D54372D", // sparkle_bronze
    ];

    [Fact]
    public void Sparkles_table_matches_reference_transcription()
    {
        var set = AnimationTables.Sparkles;
        Assert.Equal("Sparkles", set.Name);
        Assert.Equal("WO_GEMS", set.FixedSheet);
        Assert.Null(set.Category);
        Assert.Equal(Names.Length, set.Anims.Count);

        for (int type = 0; type < Names.Length; type++)
        {
            var anim = set.Anims[type];
            Assert.Equal(Names[type], anim.Name);
            Assert.Equal(AnimMode.Loop, anim.Mode);
            Assert.Equal(Enumerable.Range(20 + type * 8, 8), anim.Frames);
            Assert.Equal(2, anim.TicksPerStep);
        }

        Assert.True(set.SlicedSheet);
        Assert.Null(set.Rects);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Sparkles_rects_derived_from_the_sheet_are_the_glint_block()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var set = AnimationTables.Sparkles;
        var pages = SheetImage.Read(library.Read("WO_GEMS.SPF")).Frames;
        var rects = SkinCatalog.RectsOf(set, pages);

        // The four corners of the block, so ids 20-91 are shown to land on the glints with no offset
        // applied anywhere. The gem cells this table does not cover are dropped rather than sizing the
        // player's stage.
        Assert.Equal(72, rects!.Count);
        Assert.Equal(new FrameRect(0, 1, 15, 7, 7), rects[20]);
        Assert.Equal(new FrameRect(0, 1 + 7 * 8, 15, 7, 7), rects[27]);
        Assert.Equal(new FrameRect(0, 1, 15 + 8 * 8, 7, 7), rects[84]);
        Assert.Equal(new FrameRect(0, 1 + 7 * 8, 15 + 8 * 8, 7, 7), rects[91]);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Sparkles_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var set = AnimationTables.Sparkles;
        var pages = SheetImage.Read(library.Read("WO_GEMS.SPF")).Frames;
        var rects = SkinCatalog.RectsOf(set, pages)!;
        var palette = VgaPalette.From6Bit(
            library.Read(set.FixedPalette!).AsSpan(0, 768));

        for (int i = 0; i < set.Anims.Count; i++)
        {
            using var sha = SHA256.Create();
            foreach (var f in set.Anims[i].Frames)
            {
                var rect = rects[f];
                var cell = SpriteSheetSlicer.RectCell(pages[rect.Page], rect.X, rect.Y,
                    rect.X + rect.W - 1, rect.Y + rect.H - 1);
                Assert.False(cell.IsEmpty, $"{set.Anims[i].Name} frame {f}: expected art, found an empty cell");
                var rgba = new byte[cell.W * cell.H * 4];
                for (int p = 0; p < cell.Pixels.Length; p++)
                {
                    byte v = cell.Pixels[p];
                    if (v == 0) continue;
                    var (r, g, b) = palette[v];
                    int o = p * 4;
                    rgba[o] = r;
                    rgba[o + 1] = g;
                    rgba[o + 2] = b;
                    rgba[o + 3] = 255;
                }
                sha.TransformBlock(rgba, 0, rgba.Length, null, 0);
            }
            sha.TransformFinalBlock([], 0, 0);
            Assert.Equal(Shas[i], Convert.ToHexString(sha.Hash!), ignoreCase: true);
        }
    }
}
