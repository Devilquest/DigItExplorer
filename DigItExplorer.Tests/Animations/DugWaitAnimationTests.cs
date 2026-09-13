using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Dug idle wait animation tables and static frame sequence hashes against reference files.</summary>
public class DugWaitAnimationTests
{
    [Fact]
    public void DugWait_table_matches_transcription()
    {
        var set = AnimationTables.DugWait;
        Assert.Equal("DugWait", set.Name);
        Assert.Null(set.Category);
        Assert.Equal('B', set.SheetLetter);
        Assert.Equal(2, set.Anims.Count);
        Assert.Equal("idle_wait_frames", set.Anims[0].Name);
        Assert.Equal(AnimMode.Loop, set.Anims[0].Mode);
        Assert.Equal("idle_wait_sim", set.Anims[1].Name);
        Assert.Equal(AnimMode.Loop, set.Anims[1].Mode);

        int[] expected =
        [
            64, 65, 64, 65, 64, 65, 64, 65,
            66, 67, 66, 67, 66, 67, 66, 67,
            68, 69, 68, 69, 68, 69, 68, 69,
            70, 71, 70, 71, 70, 71, 70, 71,
            72, 73, 72, 73, 72, 73, 72, 73,
            74, 75, 74, 75, 74, 75, 74, 75,
        ];
        Assert.Equal(expected, set.Anims[0].Frames);
    }

    // Same sheets as DugCrouch, both being on sheet B: one SHA-256 per costume over the wait frame list.
    private static readonly (string Sheet, string Pal, string Sha)[] Reference =
    [
        ("DUG0B.SPF", "LVL000.PAL", "B36E8AC43FD13E8C8549467CAD6D2EEA0324CB6500BB9E6DFBE47A3C0ECE3A4A"),
        ("DUG6B.SPF", "LVL400.PAL", "A01743FF60F11BF788F8EA6194DD430BFE2B5250A6B9A19B0E73192F4876213A"),
        ("DUG3B.SPF", "LVL600.PAL", "6BCF9A090B66DBEB2090D278AC09D3A4FF6C414756F9F9F2FD3D55059AA981D5"),
        ("DUG5B.SPF", "LVL000.PAL", "ED13EF144C67051E22E7100EADD666490FD86B48C213E85879C89782E06AC169"),
    ];

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void DugWait_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var set = AnimationTables.DugWait;
        foreach (var (sheetFile, palName, expectedSha) in Reference)
        {
            var pages = SheetImage.Read(library.Read(sheetFile)).Frames;
            var palette = VgaPalette.From6Bit(library.Read(palName).AsSpan(0, 768));

            var cells = new Dictionary<int, SpriteCell>();
            foreach (var (frameId, rect) in set.Rects!)
                cells[frameId] = SpriteSheetSlicer.RectCell(pages[rect.Page], rect.X, rect.Y, rect.X + rect.W - 1, rect.Y + rect.H - 1);

            using var sha = SHA256.Create();
            foreach (var f in set.Anims[0].Frames)
            {
                var cell = cells[f];
                var rgba = new byte[cell.W * cell.H * 4];
                AnimationPixels.ToRgba(cell, palette, rgba);
                sha.TransformBlock(rgba, 0, rgba.Length, null, 0);
            }
            sha.TransformFinalBlock([], 0, 0);
            Assert.Equal(expectedSha, Convert.ToHexString(sha.Hash!), ignoreCase: true);
        }
    }
}
