using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Tests;

/// <summary>Shared assertions for the animation guard tests, which pin each character's transcribed
/// table and rendered frame hashes.</summary>
internal static class AnimationGuard
{
    /// <summary>Asserts a set's animations match the expected transcription entry for entry.</summary>
    /// <param name="set">The animation set under test.</param>
    /// <param name="expected">Expected name, frame list, and playback mode per animation, in table order.</param>
    public static void AssertTable(CharacterAnimSet set,
        IReadOnlyList<(string Name, int[] Frames, AnimMode Mode)> expected)
    {
        Assert.Equal(expected.Count, set.Anims.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Name, set.Anims[i].Name);
            Assert.Equal(expected[i].Frames, set.Anims[i].Frames);
            Assert.Equal(expected[i].Mode, set.Anims[i].Mode);
        }
    }

    /// <summary>Asserts every animation's frames hash to their pinned reference, with cells cut from
    /// the set's own transcribed rects.</summary>
    /// <param name="set">The animation set under test, which must carry rects.</param>
    /// <param name="pages">Decoded sheet pages the cells are cut from.</param>
    /// <param name="palette">Palette the frames are rendered through.</param>
    /// <param name="shas">Expected SHA-256 per animation, in table order.</param>
    public static void AssertRectHashes(CharacterAnimSet set, IReadOnlyList<byte[]> pages, VgaPalette palette,
        IReadOnlyList<string> shas)
    {
        var cells = new Dictionary<int, SpriteCell>();
        foreach (var (frameId, rect) in set.Rects!)
            cells[frameId] = SpriteSheetSlicer.RectCell(pages[rect.Page], rect.X, rect.Y,
                rect.X + rect.W - 1, rect.Y + rect.H - 1);

        for (int i = 0; i < set.Anims.Count; i++)
        {
            using var sha = SHA256.Create();
            foreach (var f in set.Anims[i].Frames)
            {
                var cell = cells[f];
                var rgba = new byte[cell.W * cell.H * 4];
                AnimationPixels.ToRgba(cell, palette, rgba);
                sha.TransformBlock(rgba, 0, rgba.Length, null, 0);
            }
            sha.TransformFinalBlock([], 0, 0);
            Assert.Equal(shas[i], Convert.ToHexString(sha.Hash!), ignoreCase: true);
        }
    }

    /// <summary>Asserts every animation's frames hash to their pinned reference, with cells cut from a
    /// uniform grid sheet, and that no animation resolves to entirely empty art.</summary>
    /// <param name="grid">Grid geometry the cells are cut on.</param>
    /// <param name="pages">Decoded sheet pages the cells are cut from.</param>
    /// <param name="palette">Palette the frames are rendered through.</param>
    /// <param name="anims">The animations under test, in table order.</param>
    /// <param name="shas">Expected SHA-256 per animation, in table order.</param>
    /// <param name="label">Skin identifier naming the sheet in assertion failures.</param>
    public static void AssertGridHashes(EnemyGrid grid, IReadOnlyList<byte[]> pages, VgaPalette palette,
        IReadOnlyList<AnimationDef> anims, IReadOnlyList<string> shas, string label)
    {
        var cells = new SpriteCell[anims.SelectMany(a => a.Frames).Max() + 1];
        for (int f = 0; f < cells.Length; f++)
            cells[f] = SpriteSheetSlicer.GridCell(pages, grid.Cols, grid.StrideX, grid.StrideY, grid.W, grid.H, f);

        for (int i = 0; i < anims.Count; i++)
        {
            bool absent = anims[i].Frames.All(f => cells[f].IsEmpty);
            Assert.False(absent, $"{label}/{anims[i].Name}: expected art, found all-empty cells");

            using var sha = SHA256.Create();
            var rgba = new byte[grid.W * grid.H * 4];
            foreach (var f in anims[i].Frames)
            {
                AnimationPixels.ToRgba(cells[f], palette, rgba);
                sha.TransformBlock(rgba, 0, rgba.Length, null, 0);
            }
            sha.TransformFinalBlock([], 0, 0);
            Assert.Equal(shas[i], Convert.ToHexString(sha.Hash!), ignoreCase: true);
        }
    }
}
