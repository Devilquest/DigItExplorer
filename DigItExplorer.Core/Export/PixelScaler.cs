namespace DigItExplorer.Core.Export;

/// <summary>Magnifies raw pixel buffers using nearest-neighbor integer scaling.</summary>
public static class PixelScaler
{
    /// <summary>Magnifies a packed pixel buffer by an integer factor along both axes.</summary>
    /// <param name="pixels">Source pixel buffer in row-major order.</param>
    /// <param name="width">Width of source image in pixels.</param>
    /// <param name="height">Height of source image in pixels.</param>
    /// <param name="bytesPerPixel">Bytes per pixel.</param>
    /// <param name="factor">Integer magnification factor (1 returns original buffer).</param>
    /// <returns>Magnified pixel buffer.</returns>
    public static byte[] Magnify(byte[] pixels, int width, int height, int bytesPerPixel, int factor)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(bytesPerPixel, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(factor, 1);

        int stride = width * bytesPerPixel;
        if (pixels.Length < stride * height)
        {
            throw new ArgumentException(
                $"Buffer holds {pixels.Length} bytes, too few for {width}×{height} at {bytesPerPixel} bytes per pixel.",
                nameof(pixels));
        }

        if (factor == 1) return pixels;

        int outStride = stride * factor;
        var result = new byte[outStride * height * factor];

        for (int y = 0; y < height; y++)
        {
            int srcRow = y * stride;
            int dstRow = y * factor * outStride;

            // Widen one row, then copy the finished row down the remaining rows of its block: the vertical
            // pass is whole-row copies rather than a second per-pixel loop.
            for (int x = 0; x < width; x++)
            {
                int src = srcRow + x * bytesPerPixel;
                int dst = dstRow + x * factor * bytesPerPixel;
                for (int repeat = 0; repeat < factor; repeat++)
                    Array.Copy(pixels, src, result, dst + repeat * bytesPerPixel, bytesPerPixel);
            }

            for (int repeat = 1; repeat < factor; repeat++)
                Array.Copy(result, dstRow, result, dstRow + repeat * outStride, outStride);
        }

        return result;
    }
}
