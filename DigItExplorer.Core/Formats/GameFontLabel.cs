namespace DigItExplorer.Core.Formats;

/// <summary>Renders <see cref="GameFont"/> strings with 1 px black outlines for on-screen labels.</summary>
internal static class GameFontLabel
{
    /// <summary>Computes the bounding pixel height occupied by the glyphs in <paramref name="text"/>.</summary>
    public static int Height(GameFont font, ReadOnlySpan<byte> text)
    {
        int lowestInkRow = 0;
        foreach (var b in text) lowestInkRow = Math.Max(lowestInkRow, font.Heights[font.Code(b)]);
        return lowestInkRow + 1;
    }

    /// <summary>Blits <paramref name="text"/> into <paramref name="mask"/> at integer magnification scale.</summary>
    public static void DrawScaled(GameFont font, byte[] mask, int canvasW, int canvasH, int x, int y,
        ReadOnlySpan<byte> text, IReadOnlyList<byte> ramp, int scale)
    {
        if (scale <= 1)
        {
            font.Draw(mask, canvasW, canvasH, x, y, text, ramp);
            return;
        }

        if (font.TextWidth(text) <= 0) return;
        int textH = Height(font, text);

        // TextWidth measures cursor advance; +1 px accommodates ink-inclusive bounds without clipping.
        int stride = font.TextWidth(text) + 1;

        // Render at 1x then expand pixels to preserve exact font spacing and stem ratios.
        var glyphs = new byte[stride * textH];
        font.Draw(glyphs, stride, textH, 0, 0, text, ramp);

        for (int gy = 0; gy < textH; gy++)
        {
            for (int gx = 0; gx < stride; gx++)
            {
                byte v = glyphs[gy * stride + gx];
                if (v == 0) continue;
                for (int dy = 0; dy < scale; dy++)
                {
                    int py = y + gy * scale + dy;
                    if (py < 0 || py >= canvasH) continue;
                    for (int dx = 0; dx < scale; dx++)
                    {
                        int px = x + gx * scale + dx;
                        if (px < 0 || px >= canvasW) continue;
                        mask[py * canvasW + px] = v;
                    }
                }
            }
        }
    }

    /// <summary>Composites a label mask onto RGB24 output applying a 1 px black outline.</summary>
    public static void Composite(byte[] rgb, byte[] coverage, byte[] mask, int canvasW, int canvasH, VgaPalette palette)
    {
        var pal = palette.Rgb;
        for (int y = 0; y < canvasH; y++)
        {
            for (int x = 0; x < canvasW; x++)
            {
                int p = y * canvasW + x;
                byte v = mask[p];

                // Outline and glyph pixels are disjoint, allowing a single pass to place both.
                if (v == 0)
                {
                    bool touchesInk = (x > 0 && mask[p - 1] != 0)
                        || (x < canvasW - 1 && mask[p + 1] != 0)
                        || (y > 0 && mask[p - canvasW] != 0)
                        || (y < canvasH - 1 && mask[p + canvasW] != 0);
                    if (!touchesInk) continue;
                    v = 0; // palette index 0, the outline's flat black
                }

                RgbCanvas.PaintIndex(rgb, coverage, p, pal, v);
            }
        }
    }
}
