using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Maps;

/// <summary>Block-grid dimensions, logical size, and missing block count for a tiled map.</summary>
/// <param name="Cols">Block-grid columns.</param>
/// <param name="Rows">Block-grid rows.</param>
/// <param name="Width">Logical width of the level in pixels.</param>
/// <param name="Height">Logical height of the level in pixels.</param>
/// <param name="MissingBlocks">Count of required grid blocks that failed to decode.</param>
internal readonly record struct MapGrid(int Cols, int Rows, int Width, int Height, int MissingBlocks)
{
    /// <summary>Blocks the grid calls for.</summary>
    public int Blocks => Cols * Rows;
}

/// <summary>Derives block-grid layout dimensions from level DLF headers.</summary>
internal static class MapGridShape
{
    public const int BlockWidth = FrameCodec.Width;   // 320
    public const int BlockHeight = FrameCodec.Height;  // 200

    /// <summary>Calculates the map block grid layout from DLF header dimensions.</summary>
    /// <param name="dlf">DLF file bytes containing the level header.</param>
    /// <param name="blocks">Number of decoded blocks available in the sheet.</param>
    /// <returns>Calculated MapGrid layout and missing block count.</returns>
    public static MapGrid Derive(byte[]? dlf, int blocks)
    {
        if (dlf is { Length: >= DlfHeader.Size })
        {
            var header = DlfHeader.Read(dlf);
            int cols = Math.Max(1, CeilDiv(header.Width, BlockWidth));
            int rows = Math.Max(1, CeilDiv(header.Height, BlockHeight));
            return new MapGrid(cols, rows, header.Width, header.Height,
                               Math.Max(0, cols * rows - blocks));
        }

        return new MapGrid(blocks, 1, blocks * BlockWidth, BlockHeight, 0);
    }

    private static int CeilDiv(int a, int b) => (a + b - 1) / b;
}
