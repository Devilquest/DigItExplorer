using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Tests;

/// <summary>Guards the RGB buffer layout every composition step draws over.</summary>
public sealed class TerrainImageTests
{
    private const int Width = 7;    // deliberately not a multiple of 4, which is where a padded stride appears
    private const int Height = 3;

    [Fact]
    public void The_rgb_buffer_carries_no_row_padding()
    {
        var rgb = Image().ToRgb24();

        // The composition steps index into this buffer arithmetically, so a stride wider than the row
        // shears every one of them and nothing in their code would say so.
        Assert.Equal(Width * Height * 3, rgb.Length);
    }

    [Fact]
    public void Each_row_starts_where_the_width_alone_puts_it()
    {
        var image = Image();
        var rgb = image.ToRgb24();

        for (int y = 0; y < Height; y++)
        {
            var expected = image.Palette.Rgb[(y + 1) * 3];
            Assert.Equal(expected, rgb[y * Width * 3]);
            Assert.Equal(expected, rgb[(y * Width + Width - 1) * 3]);
        }
    }

    // One palette entry per row, so a row read at the wrong offset comes back as a different color.
    private static TerrainImage Image()
    {
        var raw = new byte[768];
        for (int index = 0; index < 4; index++) raw[index * 3] = (byte)(index * 20);

        var indices = new byte[Width * Height];
        for (int y = 0; y < Height; y++)
            indices.AsSpan(y * Width, Width).Fill((byte)(y + 1));

        return new TerrainImage(Width, Height, cols: 1, rows: 1, blockCount: 1, missingBlocks: 0,
            VgaPalette.From6Bit(raw), indices);
    }
}
