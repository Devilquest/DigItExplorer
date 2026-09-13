namespace DigItExplorer.Core.Maps;

/// <summary>Reconstructed level collision layer containing stitched raw material codes.</summary>
public sealed class CollisionImage
{
    /// <summary>Cropped playfield width in pixels.</summary>
    public int Width { get; }

    /// <summary>Cropped playfield height in pixels.</summary>
    public int Height { get; }

    /// <summary>Block-grid columns stitched.</summary>
    public int Cols { get; }

    /// <summary>Block-grid rows stitched.</summary>
    public int Rows { get; }

    /// <summary>Total material block count in the stitched grid (Cols * Rows).</summary>
    public int BlockCount { get; }

    /// <summary>Count of grid blocks that failed to decode from the sheet.</summary>
    public int MissingBlocks { get; }

    /// <summary>Row-major raw material codes of the cropped playfield.</summary>
    public byte[] MaterialCodes { get; }

    internal CollisionImage(int width, int height, int cols, int rows, int blockCount, int missingBlocks,
                            byte[] materialCodes)
    {
        Width = width;
        Height = height;
        Cols = cols;
        Rows = rows;
        BlockCount = blockCount;
        MissingBlocks = missingBlocks;
        MaterialCodes = materialCodes;
    }

    /// <summary>Converts raw material codes to a 32-bit RGBA buffer through MaterialPalette.</summary>
    public byte[] ToRgba32()
    {
        var rgba = new byte[Width * Height * 4];
        for (int i = 0; i < MaterialCodes.Length; i++)
        {
            var (r, g, b, a) = MaterialPalette.ColorOf(MaterialCodes[i]);
            int o = i * 4;
            rgba[o] = r;
            rgba[o + 1] = g;
            rgba[o + 2] = b;
            rgba[o + 3] = a;
        }
        return rgba;
    }
}
