using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins unused fireball animation tables across all world variants against reference files.</summary>
public class FireballUnusedAnimationTests
{
    private static readonly (string Suffix, string Pal, string[] Shas)[] Reference =
    [
        ("00", "LVL000.PAL",
        [
            "59702F2BE7FC859B3AE37B98DE2A0BFF92CB38956685B8D7D126B22FF60604B7", // flight_intro
            "89A145347F757ACD1031A9D505249DCC00ADE39095EBF379803F6158A061E278", // flight_loop
            "248DA68FB5440A72F09D7E9BD5094E48CEF8F4CA962D5D0919AECEE02902C0F6", // impact
        ]),
        ("02", "LVL400.PAL",
        [
            "027A752746FE30FBAA9B1AB2CB50BDFB8052421D89D54DBCD0E5F5A70F9DE813", // flight_intro
            "73D6B22A1B00C1C30749E9D6D50C84F47431A764EA6498D305385F7C1D9AC5B7", // flight_loop
            "248DA68FB5440A72F09D7E9BD5094E48CEF8F4CA962D5D0919AECEE02902C0F6", // impact
        ]),
        ("03", "LVL600.PAL",
        [
            "59702F2BE7FC859B3AE37B98DE2A0BFF92CB38956685B8D7D126B22FF60604B7", // flight_intro
            "89A145347F757ACD1031A9D505249DCC00ADE39095EBF379803F6158A061E278", // flight_loop
            "248DA68FB5440A72F09D7E9BD5094E48CEF8F4CA962D5D0919AECEE02902C0F6", // impact
        ]),
    ];

    [Fact]
    public void FireballUnused_mirrors_the_real_fireball_one_page_over()
    {
        var unused = AnimationTables.FireballUnused;
        var real = AnimationTables.Fireball;

        Assert.Equal("Fireball (unused)", unused.Name);
        Assert.Equal((byte)0x09, unused.Category); // Pyrosaur's, purely to resolve WO_REDxx
        Assert.Null(unused.FixedSheet);

        // Same frame lists and same layout as the shipping fireball, one page over.
        Assert.Equal(real.Anims.Count, unused.Anims.Count);
        for (int i = 0; i < real.Anims.Count; i++)
        {
            Assert.Equal(real.Anims[i].Name, unused.Anims[i].Name);
            Assert.Equal(real.Anims[i].Frames, unused.Anims[i].Frames);
            Assert.Equal(real.Anims[i].Mode, unused.Anims[i].Mode);
        }
        foreach (var (frame, rect) in real.Rects!)
            Assert.Equal(rect with { Page = 1 }, unused.Rects![frame]);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void FireballUnused_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var set = AnimationTables.FireballUnused;
        Assert.True(EnemyGrids.TryGet(set.Category!.Value, out var grid)); // sheet-name resolution only

        foreach (var (suffix, palName, shas) in Reference)
        {
            var pages = SheetImage.Read(library.Read($"{grid.Sheet}{suffix}.MPF")).Frames;
            var palette = VgaPalette.From6Bit(library.Read(palName).AsSpan(0, 768));

            for (int i = 0; i < set.Anims.Count; i++)
            {
                using var sha = SHA256.Create();
                foreach (var f in set.Anims[i].Frames)
                {
                    var rect = set.Rects![f];
                    var cell = SpriteSheetSlicer.RectCell(pages[rect.Page], rect.X, rect.Y,
                        rect.X + rect.W - 1, rect.Y + rect.H - 1);
                    var rgba = new byte[cell.W * cell.H * 4];
                    AnimationPixels.ToRgba(cell, palette, rgba);
                    sha.TransformBlock(rgba, 0, rgba.Length, null, 0);
                }
                sha.TransformFinalBlock([], 0, 0);
                Assert.Equal(shas[i], Convert.ToHexString(sha.Hash!), ignoreCase: true);
            }
        }
    }

    [Fact]
    public void Snow_copy_differs_while_caves_and_underworld_are_identical()
    {
        // The whole reason this leftover is cataloged rather than just noted: WO_RED00 and WO_RED03 carry
        // the same copy, WO_RED02 carries a different (mirrored/rotated) spin, and all three share the
        // impact explosion, which is also byte-identical to the shipping WO_FBALL.SPF's own.
        var caves = Reference[0].Shas;
        var snow = Reference[1].Shas;
        var underworld = Reference[2].Shas;

        Assert.Equal(caves[0], underworld[0]);
        Assert.Equal(caves[1], underworld[1]);
        Assert.NotEqual(caves[0], snow[0]);
        Assert.NotEqual(caves[1], snow[1]);
        Assert.Equal(caves[2], snow[2]);
    }
}
