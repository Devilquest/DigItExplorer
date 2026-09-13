using DigItExplorer.Core.Formats;

namespace DigItExplorer.Tests;

/// <summary>Verifies sprite overlay composition in <see cref="SpriteCell.Over"/>.</summary>
public class SpriteCellOverTests
{
    private static SpriteCell Cell(int w, int h, byte fill) =>
        new(0, 0, w, h, Enumerable.Repeat(fill, w * h).ToArray());

    [Fact]
    public void Composed_cell_keeps_the_underlays_position_and_size()
    {
        var composed = Cell(2, 2, 7).Over(new SpriteCell(4, 5, 6, 3, new byte[18]), 1, 0);

        Assert.Equal((4, 5, 6, 3), (composed.X, composed.Y, composed.W, composed.H));
        Assert.Equal(18, composed.Pixels.Length);
    }

    [Fact]
    public void Overlay_lands_at_the_offset_and_leaves_the_rest_of_the_underlay_alone()
    {
        var composed = Cell(2, 1, 7).Over(Cell(5, 2, 3), 2, 1);

        Assert.Equal<byte[]>([3, 3, 3, 3, 3,
                              3, 3, 7, 7, 3], composed.Pixels);
    }

    [Fact]
    public void Transparent_overlay_pixels_let_the_underlay_show_through()
    {
        // Index 0 always, plus whatever the sheet keys out: index 1 for the HUD's own field.
        var overlay = new SpriteCell(0, 0, 3, 1, [0, 1, 7]);

        var composed = overlay.Over(Cell(3, 1, 3), 0, 0, new HashSet<byte> { 1 });

        Assert.Equal<byte[]>([3, 3, 7], composed.Pixels);
    }

    [Fact]
    public void Overlay_pixels_falling_outside_the_underlay_are_dropped_rather_than_wrapping()
    {
        var composed = Cell(3, 3, 7).Over(Cell(2, 2, 3), 1, 1);

        Assert.Equal<byte[]>([3, 3,
                              3, 7], composed.Pixels);
    }

    [Fact]
    public void Composing_does_not_mutate_the_underlay()
    {
        var under = Cell(2, 1, 3);
        Cell(2, 1, 7).Over(under, 0, 0);

        Assert.Equal<byte[]>([3, 3], under.Pixels);
    }
}
