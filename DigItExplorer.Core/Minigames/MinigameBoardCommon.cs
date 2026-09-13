using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Core.Minigames;

/// <summary>Composed 24-bit RGB minigame board frame canvas and its coverage plane.</summary>
public sealed record MinigameBoardFrame(byte[] Rgb, byte[] Alpha, int Width, int Height);

/// <summary>Common pixel and blitting utilities for minigame board compositors.</summary>
internal static class MinigameBoardCommon
{
    /// <summary>Converts indexed 8-bit image pixels to a 24-bit RGB buffer.</summary>
    public static byte[] ToRgb(byte[] indices, VgaPalette palette)
    {
        var rgb = new byte[indices.Length * 3];
        var pal = palette.Rgb;
        for (int p = 0; p < indices.Length; p++)
        {
            int pi = indices[p] * 3, o = p * 3;
            rgb[o] = pal[pi];
            rgb[o + 1] = pal[pi + 1];
            rgb[o + 2] = pal[pi + 2];
        }
        return rgb;
    }

    /// <summary>Blits a sprite cell onto an RGB canvas with palette index transparency.</summary>
    public static void Blit(byte[] rgb, byte[] coverage, int canvasW, int canvasH, int x0, int y0, SpriteCell cell,
        VgaPalette palette, IReadOnlySet<byte>? alsoTransparent = null)
    {
        var pal = palette.Rgb;
        foreach (var (dst, v) in new ClippedBlit(cell, x0, y0, canvasW, canvasH))
        {
            if (v == 0 || alsoTransparent?.Contains(v) == true) continue;

            RgbCanvas.PaintIndex(rgb, coverage, dst, pal, v);
        }
    }

    /// <summary>Stamps the world-themed attempts icons onto the board canvas.</summary>
    public static void StampAttempts(byte[] rgb, byte[] coverage, MinigameBoard board, VgaPalette boardPalette,
        Func<string, byte[]?> loadResource)
    {
        var gsBytes = loadResource("WO_G&S.SPF");
        if (gsBytes is null) return;

        var pages = SheetImage.Read(gsBytes).Frames;
        var rects = SkinCatalog.RectsOf(AnimationTables.GoldItems, pages);
        int frame = MinigameBoardPositions.AttemptsIconFrame[board];
        if (rects is null || !rects.TryGetValue(frame, out var rect)) return;

        var cell = SpriteSheetSlicer.RectCell(pages[rect.Page], rect.X, rect.Y, rect.X + rect.W - 1, rect.Y + rect.H - 1);
        foreach (int y in MinigameBoardPositions.AttemptsIconY)
            Blit(rgb, coverage, MinigameBoardPositions.Width, MinigameBoardPositions.Height,
                MinigameBoardPositions.AttemptsIconX, y, cell, boardPalette);
    }
}
