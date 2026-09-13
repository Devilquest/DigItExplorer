using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Maps;

/// <summary>Two-plane sprite canvas maintaining parallel RGB and per-pixel alpha buffers.</summary>
internal sealed class SpriteCanvas(int width, int height)
{
    /// <summary>Composed RGB color buffer (3 bytes per pixel).</summary>
    public byte[] Rgb { get; } = new byte[width * height * 3];

    /// <summary>Per-pixel alpha coverage buffer (1 byte per pixel).</summary>
    public byte[] Alpha { get; } = new byte[width * height];

    /// <summary>Composites a source pixel over the canvas using un-premultiplied alpha blending.</summary>
    public void Blend(int pixel, byte r, byte g, byte b, byte a)
        => RgbCanvas.Blend(Rgb, Alpha, pixel, r, g, b, a);

    /// <summary>Draws an indexed sprite cell opaquely, skipping transparent index 0.</summary>
    public void DrawIndexed(SpriteCell cell, VgaPalette palette)
        => DrawIndexed(cell, palette, static index => index != 0);

    /// <summary>Draws an indexed sprite cell opaquely, keeping the pixels the predicate reports as drawn.</summary>
    public void DrawIndexed(SpriteCell cell, VgaPalette palette, Func<byte, bool> isDrawn)
    {
        for (int i = 0; i < cell.Pixels.Length; i++)
        {
            byte index = cell.Pixels[i];
            if (!isDrawn(index)) continue;
            var (r, g, b) = palette[index];
            Blend(i, r, g, b, 255);
        }
    }
}
