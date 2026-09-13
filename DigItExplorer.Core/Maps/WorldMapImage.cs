using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Maps;

/// <summary>Reconstructed world map base layer chained into a single row of blocks.</summary>
internal sealed class WorldMapImage
{
    /// <summary>Canvas width in pixels (BlockCount * 320).</summary>
    public int Width { get; }

    /// <summary>Canvas height in pixels (200).</summary>
    public int Height { get; }

    /// <summary>Number of 320x200 blocks chained horizontally after trimming.</summary>
    public int BlockCount { get; }

    /// <summary>VGA palette used to color the indexed canvas pixels.</summary>
    public VgaPalette Palette { get; }

    /// <summary>Row-major palette indices of the canvas.</summary>
    public byte[] Indices { get; }

    internal WorldMapImage(int width, int height, int blockCount, VgaPalette palette, byte[] indices)
    {
        Width = width;
        Height = height;
        BlockCount = blockCount;
        Palette = palette;
        Indices = indices;
    }

    /// <summary>Converts the indexed world map image to a row-major 24-bit RGB buffer.</summary>
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
