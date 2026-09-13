using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Maps;

/// <summary>Decodes chained MAPxx*.MPF block sheets into single-row canvas buffers.</summary>
internal static class WorldMapPlane
{
    private const int BlockWidth = MapGridShape.BlockWidth;   // 320
    private const int BlockHeight = MapGridShape.BlockHeight; // 200

    /// <summary>Decodes an MPF sheet into raw row-major canvas bytes with trailing empty blocks trimmed.</summary>
    public static (int Width, int Height, int Blocks, byte[] Indices) Decode(byte[] mpf)
    {
        var sheet = SheetImage.Read(mpf);
        var frames = TrimTrailingEmptyBlocks(sheet.Frames);
        int blocks = frames.Count;
        if (blocks == 0)
            throw new ArgumentException("MPF decoded to zero blocks.", nameof(mpf));

        // No DLF ever backs a world map, so this always falls back to the single-row case
        // (cols=blocks, rows=1, width=blocks*320, height=200): the exact chain layout world maps use.
        var grid = MapGridShape.Derive(null, blocks);
        int cols = grid.Cols, width = grid.Width, height = grid.Height;

        var canvas = new byte[width * height];
        for (int i = 0; i < blocks; i++)
        {
            var buf = frames[i];
            int cx = (i % cols) * BlockWidth;
            int cy = (i / cols) * BlockHeight;
            for (int y = 0; y < BlockHeight; y++)
            {
                int srcOff = y * BlockWidth;
                int dstOff = (cy + y) * width + cx;
                buf.AsSpan(srcOff, BlockWidth).CopyTo(canvas.AsSpan(dstOff, BlockWidth));
            }
        }

        return (width, height, blocks, canvas);
    }

    /// <summary>Trims trailing all-zero empty frames from a block list, keeping at least one.</summary>
    private static IReadOnlyList<byte[]> TrimTrailingEmptyBlocks(IReadOnlyList<byte[]> frames)
    {
        int count = frames.Count;
        while (count > 1 && Array.TrueForAll(frames[count - 1], b => b == 0))
            count--;
        return count == frames.Count ? frames : [.. frames.Take(count)];
    }
}
