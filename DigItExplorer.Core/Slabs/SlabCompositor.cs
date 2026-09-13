using System.Text;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Minigames;

namespace DigItExplorer.Core.Slabs;

/// <summary>Layer visibility options for slab screen rendering.</summary>
public sealed record SlabRenderOptions(bool ShowBackground, bool ShowForeground, bool ShowFrame, bool ShowText);

/// <summary>Rendered slab screen RGB canvas, its coverage plane, and actual visibility status for each layer.</summary>
public sealed record SlabRenderResult(byte[] Rgb, byte[] Alpha, int Width, int Height,
    bool ShowedBackground, bool ShowedForeground, bool ShowedFrame, bool ShowedText);

/// <summary>Composes resting camera stops of slab instruction and credit screens.</summary>
public static class SlabCompositor
{
    private const int ParaYOffset = -28;

    private const int IndicatorCenterX = 160;
    private const int IndicatorTopY = 180;

    /// <summary>Composes a slab screen stop from game resources and layer visibility options.</summary>
    public static SlabRenderResult? Compose(Func<string, byte[]?> loadResource, GameFont font, SlabData data,
        int screen, int stopIndex, SlabRenderOptions options)
    {
        if (!options.ShowBackground && !options.ShowForeground && !options.ShowFrame && !options.ShowText)
            return null;

        string prefix = $"SLB{screen:00}";

        var palBytes = loadResource($"{prefix}.PAL");
        if (palBytes is null || palBytes.Length < 768) return null;
        var palette = VgaPalette.From6Bit(palBytes.AsSpan(0, 768));

        var fBytes = loadResource($"{prefix}F.MPF");
        if (fBytes is null) return null;
        var stitched = Stitch(SheetImage.Read(fBytes).Frames, data.Width, data.Height);

        var frmBytes = loadResource($"{prefix}FRM.SPF");
        if (frmBytes is null) return null;
        var frmFrames = SheetImage.Read(frmBytes).Frames;
        if (frmFrames.Count == 0) return null;
        var frame = frmFrames[0];

        int w = FrameCodec.Width, h = FrameCodec.Height;
        int camY = stopIndex * SlabData.SlabStep;

        var rgb = new byte[w * h * 3];
        var coverage = new byte[w * h];

        bool showedBackground = false;
        if (options.ShowBackground)
        {
            var paraBytes = loadResource(data.ParallaxFile);
            var paraFrames = paraBytes is not null ? SheetImage.Read(paraBytes).Frames : null;
            if (paraFrames is { Count: > 0 })
            {
                var sampled = SampleBackdrop(paraFrames[0], w, h, camY);
                rgb = MinigameBoardCommon.ToRgb(sampled, palette);
                coverage = RgbCanvas.FullCoverage(w, h);
                showedBackground = true;
            }
        }

        if (options.ShowForeground)
        {
            var crop = Crop(stitched, data.Width, data.Height, camY, w, h);
            Overlay(rgb, coverage, crop, palette, v => v == 0);
        }

        if (options.ShowFrame)
            Overlay(rgb, coverage, frame, palette, v => v is 0 or 1);

        if (options.ShowText)
            DrawIndicator(rgb, coverage, font, data, screen, stopIndex, palette, w, h);

        return new SlabRenderResult(rgb, coverage, w, h, showedBackground, options.ShowForeground, options.ShowFrame,
            options.ShowText);
    }

    private static byte[] Stitch(IReadOnlyList<byte[]> pages, int width, int height)
    {
        var canvas = new byte[width * height];
        int offset = 0;
        foreach (var page in pages)
        {
            int count = Math.Min(page.Length, canvas.Length - offset);
            if (count <= 0) break;
            Array.Copy(page, 0, canvas, offset, count);
            offset += count;
        }
        return canvas;
    }

    private static byte[] Crop(byte[] canvas, int canvasWidth, int canvasHeight, int camY, int w, int h)
    {
        var crop = new byte[w * h];
        for (int y = 0; y < h; y++)
        {
            int srcRow = camY + y;
            if (srcRow < 0 || srcRow >= canvasHeight) continue;
            Array.Copy(canvas, srcRow * canvasWidth, crop, y * w, w);
        }
        return crop;
    }

    private static byte[] SampleBackdrop(byte[] backdrop, int w, int h, int camY)
    {
        var sampled = new byte[w * h];
        for (int y = 0; y < h; y++)
        {
            int srcRow = Mod(y + camY / 2 + ParaYOffset, h);
            Array.Copy(backdrop, srcRow * w, sampled, y * w, w);
        }
        return sampled;
    }

    private static int Mod(int value, int modulus) => ((value % modulus) + modulus) % modulus;

    private static void Overlay(byte[] rgb, byte[] coverage, byte[] indices, VgaPalette palette,
        Func<byte, bool> isTransparent)
    {
        var pal = palette.Rgb;
        for (int p = 0; p < indices.Length; p++)
        {
            byte v = indices[p];
            if (isTransparent(v)) continue;
            RgbCanvas.PaintIndex(rgb, coverage, p, pal, v);
        }
    }

    private static void DrawIndicator(byte[] rgb, byte[] coverage, GameFont font, SlabData data, int screen, int stopIndex,
        VgaPalette palette, int w, int h)
    {
        var text = new byte[data.LabelPrefix.Length + data.LabelInfix.Length + 8];
        int cursor = 0;
        Array.Copy(data.LabelPrefix, 0, text, cursor, data.LabelPrefix.Length);
        cursor += data.LabelPrefix.Length;
        cursor += WriteDigits(text, cursor, stopIndex + 1);
        Array.Copy(data.LabelInfix, 0, text, cursor, data.LabelInfix.Length);
        cursor += data.LabelInfix.Length;
        cursor += WriteDigits(text, cursor, data.SlabCount(screen));

        var mask = new byte[w * h];
        font.DrawCentered(mask, w, h, IndicatorCenterX, IndicatorTopY, text.AsSpan(0, cursor), GameFont.TextRamp);
        GameFontLabel.Composite(rgb, coverage, mask, w, h, palette);
    }

    private static int WriteDigits(byte[] buffer, int offset, int value)
    {
        var digits = Encoding.ASCII.GetBytes(value.ToString());
        Array.Copy(digits, 0, buffer, offset, digits.Length);
        return digits.Length;
    }
}
