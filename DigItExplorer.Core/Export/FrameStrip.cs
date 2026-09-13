namespace DigItExplorer.Core.Export;

/// <summary>Composes multiple equally sized animation frames into a single horizontal strip.</summary>
public static class FrameStrip
{
    /// <summary>Joins frames side by side into a contiguous horizontal buffer.</summary>
    /// <param name="frames">Sequence of frame byte buffers.</param>
    /// <param name="width">Width of each individual frame in pixels.</param>
    /// <param name="height">Height of each individual frame in pixels.</param>
    /// <param name="bytesPerPixel">Bytes per pixel (1 for indexed, 3 for RGB, 4 for RGBA).</param>
    /// <returns>Contiguous strip buffer.</returns>
    public static byte[] Compose(IReadOnlyList<byte[]> frames, int width, int height, int bytesPerPixel)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentOutOfRangeException.ThrowIfLessThan(frames.Count, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(bytesPerPixel, 1);

        int frameStride = width * bytesPerPixel;
        for (int f = 0; f < frames.Count; f++)
        {
            if (frames[f].Length < frameStride * height)
            {
                throw new ArgumentException(
                    $"Frame {f} holds {frames[f].Length} bytes, too few for {width}×{height} at " +
                    $"{bytesPerPixel} bytes per pixel.", nameof(frames));
            }
        }

        int stripStride = frameStride * frames.Count;
        var strip = new byte[stripStride * height];

        // Row by row across every frame, rather than frame by frame: a frame's rows are scattered through
        // the output, so this is the order that reads and writes both forward.
        for (int y = 0; y < height; y++)
        {
            int destRow = y * stripStride;
            for (int f = 0; f < frames.Count; f++)
                Array.Copy(frames[f], y * frameStride, strip, destRow + f * frameStride, frameStride);
        }

        return strip;
    }
}
