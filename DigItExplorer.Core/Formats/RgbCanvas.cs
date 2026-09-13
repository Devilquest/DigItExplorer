namespace DigItExplorer.Core.Formats;

/// <summary>Paints RGB24 canvases that carry a parallel coverage plane, so pixels no layer painted stay transparent.</summary>
internal static class RgbCanvas
{
    /// <summary>Coverage value of a fully painted pixel.</summary>
    public const byte Covered = 255;

    /// <summary>Creates a coverage plane marking every pixel as painted.</summary>
    public static byte[] FullCoverage(int width, int height)
    {
        var coverage = new byte[width * height];
        Array.Fill(coverage, Covered);
        return coverage;
    }

    /// <summary>Writes an opaque color at a pixel and marks it painted.</summary>
    public static void Paint(byte[] rgb, byte[] coverage, int pixel, byte r, byte g, byte b)
    {
        int o = pixel * 3;
        rgb[o] = r;
        rgb[o + 1] = g;
        rgb[o + 2] = b;
        coverage[pixel] = Covered;
    }

    /// <summary>Writes a palette index opaquely at a pixel and marks it painted.</summary>
    public static void PaintIndex(byte[] rgb, byte[] coverage, int pixel, ReadOnlySpan<byte> palette, byte index)
    {
        int p = index * 3;
        Paint(rgb, coverage, pixel, palette[p], palette[p + 1], palette[p + 2]);
    }

    /// <summary>Composites a translucent color over a pixel using un-premultiplied alpha blending.</summary>
    public static void Blend(byte[] rgb, byte[] coverage, int pixel, byte r, byte g, byte b, byte a)
    {
        if (a == 0) return;

        int dst = coverage[pixel];
        int outA = a * 255 + dst * (255 - a);
        int o = pixel * 3;

        rgb[o] = (byte)((r * a * 255 + rgb[o] * dst * (255 - a)) / outA);
        rgb[o + 1] = (byte)((g * a * 255 + rgb[o + 1] * dst * (255 - a)) / outA);
        rgb[o + 2] = (byte)((b * a * 255 + rgb[o + 2] * dst * (255 - a)) / outA);
        coverage[pixel] = (byte)(outA / 255);
    }
}
