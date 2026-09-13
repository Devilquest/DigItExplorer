using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Core.Ending;

/// <summary>Layer visibility options for ending sequence screen rendering.</summary>
public sealed record EndSequenceRenderOptions(bool ShowBackground, bool ShowText);

/// <summary>Rendered ending sequence RGB canvas, its coverage plane, and layer visibility status.</summary>
public sealed record EndSequenceRenderResult(byte[] Rgb, byte[] Alpha, int Width, int Height,
    bool ShowedBackground, bool ShowedText);

/// <summary>Composes static ending sequence screens with background frames and text captions.</summary>
public static class EndSequenceCompositor
{
    /// <summary>Composes an ending sequence screen from an MPF frame and caption metadata.</summary>
    /// <param name="endSeq">Loaded ending sequence MPF sheet.</param>
    /// <param name="font">Game font used for drawing caption text.</param>
    /// <param name="screenIndex">Screen index (0 to 9).</param>
    /// <param name="data">Caption layout and text metadata.</param>
    /// <param name="options">Layer visibility options.</param>
    /// <returns>Composed EndSequenceRenderResult, or null if no layers are visible.</returns>
    public static EndSequenceRenderResult? Compose(SheetImage endSeq, GameFont font, int screenIndex,
        EndSequenceData data, EndSequenceRenderOptions options)
    {
        bool showBackground = options.ShowBackground;
        bool showText = options.ShowText;
        if (!showBackground && !showText) return null;

        int w = FrameCodec.Width, h = FrameCodec.Height;
        var pal = endSeq.Palette.Rgb;
        byte[] rgb = showBackground ? ToRgb(endSeq.Frames[screenIndex], pal, w, h) : new byte[w * h * 3];
        byte[] coverage = showBackground ? RgbCanvas.FullCoverage(w, h) : new byte[w * h];

        if (showText)
        {
            var mask = new byte[w * h];
            foreach (var caption in data.Frames[screenIndex])
                Draw(mask, font, caption);
            for (int p = 0; p < mask.Length; p++)
            {
                byte v = mask[p];
                if (v == 0) continue;
                RgbCanvas.PaintIndex(rgb, coverage, p, pal, v);
            }
        }

        return new EndSequenceRenderResult(rgb, coverage, w, h, showBackground, showText);
    }

    private static byte[] ToRgb(byte[] indices, ReadOnlySpan<byte> pal, int w, int h)
    {
        var rgb = new byte[w * h * 3];
        for (int p = 0; p < indices.Length; p++)
        {
            int pi = indices[p] * 3, o = p * 3;
            rgb[o] = pal[pi];
            rgb[o + 1] = pal[pi + 1];
            rgb[o + 2] = pal[pi + 2];
        }
        return rgb;
    }

    private static void Draw(byte[] frame, GameFont font, EndSequenceCaption caption)
    {
        var ramp = GameFont.Ramp(caption.Style);
        switch (caption.Align)
        {
            case TextAlign.Left:
                font.Draw(frame, FrameCodec.Width, FrameCodec.Height, caption.X, caption.Y, caption.Text, ramp);
                break;
            case TextAlign.Center:
                font.DrawCentered(frame, FrameCodec.Width, FrameCodec.Height, caption.X, caption.Y, caption.Text, ramp);
                break;
            case TextAlign.Right:
                font.DrawRightAligned(frame, FrameCodec.Width, FrameCodec.Height, caption.X, caption.Y, caption.Text, ramp);
                break;
        }
    }
}
