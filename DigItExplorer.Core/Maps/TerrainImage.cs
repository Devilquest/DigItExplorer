using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Maps;

/// <summary>Reconstructed level terrain layer stitched from tiled MPF blocks and cropped to logical size.</summary>
public sealed class TerrainImage
{
    /// <summary>Cropped playfield width in pixels.</summary>
    public int Width { get; }

    /// <summary>Cropped playfield height in pixels.</summary>
    public int Height { get; }

    /// <summary>Block-grid columns stitched.</summary>
    public int Cols { get; }

    /// <summary>Block-grid rows stitched.</summary>
    public int Rows { get; }

    /// <summary>Total block count in the stitched grid (Cols * Rows).</summary>
    public int BlockCount { get; }

    /// <summary>Count of grid blocks that failed to decode from the sheet.</summary>
    public int MissingBlocks { get; }

    /// <summary>VGA palette used to color the indexed terrain pixels.</summary>
    public VgaPalette Palette { get; }

    /// <summary>Row-major palette indices of the cropped playfield.</summary>
    public byte[] Indices { get; }

    internal TerrainImage(int width, int height, int cols, int rows, int blockCount, int missingBlocks,
                          VgaPalette palette, byte[] indices)
    {
        Width = width;
        Height = height;
        Cols = cols;
        Rows = rows;
        BlockCount = blockCount;
        MissingBlocks = missingBlocks;
        Palette = palette;
        Indices = indices;
    }

    /// <summary>Converts the indexed terrain image to a row-major 24-bit RGB buffer.</summary>
    public byte[] ToRgb24()
    {
        var rgb = new byte[Width * Height * 3];
        var pal = Palette.Rgb;
        for (int i = 0; i < Indices.Length; i++)
        {
            int p = Indices[i] * 3, o = i * 3;
            rgb[o] = pal[p];
            rgb[o + 1] = pal[p + 1];
            rgb[o + 2] = pal[p + 2];
        }
        return rgb;
    }
}
