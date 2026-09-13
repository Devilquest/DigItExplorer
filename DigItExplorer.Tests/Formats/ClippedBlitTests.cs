using DigItExplorer.Core.Formats;

namespace DigItExplorer.Tests;

public class ClippedBlitTests
{
    // Pixels 1..4 read top-left, top-right, bottom-left, bottom-right.
    private static SpriteCell Cell2x2() => new(0, 0, 2, 2, [1, 2, 3, 4]);

    private static List<(int Dst, byte Value)> Walk(SpriteCell cell, int x0, int y0, int w, int h,
        bool mirrored = false)
    {
        var yielded = new List<(int, byte)>();
        foreach (var (dst, value) in new ClippedBlit(cell, x0, y0, w, h, mirrored))
            yielded.Add((dst, value));
        return yielded;
    }

    [Fact]
    public void An_unclipped_blit_yields_every_pixel_in_row_major_order()
    {
        Assert.Equal([(0, 1), (1, 2), (2, 3), (3, 4)], Walk(Cell2x2(), 0, 0, 2, 2));
    }

    [Fact]
    public void Destination_indices_are_row_major_on_the_target_width_not_the_cell_width()
    {
        // A 2x2 cell one pixel in from the left of a 4x4 target: rows land 4 apart, not 2.
        Assert.Equal([(5, 1), (6, 2), (9, 3), (10, 4)], Walk(Cell2x2(), 1, 1, 4, 4));
    }

    [Fact]
    public void Mirroring_reverses_each_row_without_moving_the_destination()
    {
        Assert.Equal([(0, 2), (1, 1), (2, 4), (3, 3)], Walk(Cell2x2(), 0, 0, 2, 2, mirrored: true));
    }

    [Fact]
    public void Columns_off_the_left_edge_are_dropped_and_the_rest_still_land()
    {
        Assert.Equal([(0, 2), (2, 4)], Walk(Cell2x2(), -1, 0, 2, 2));
    }

    [Fact]
    public void Columns_off_the_right_edge_are_dropped()
    {
        Assert.Equal([(1, 1), (3, 3)], Walk(Cell2x2(), 1, 0, 2, 2));
    }

    [Fact]
    public void Rows_off_the_top_edge_are_dropped()
    {
        Assert.Equal([(0, 3), (1, 4)], Walk(Cell2x2(), 0, -1, 2, 2));
    }

    [Fact]
    public void Rows_off_the_bottom_edge_are_dropped()
    {
        Assert.Equal([(2, 1), (3, 2)], Walk(Cell2x2(), 0, 1, 2, 2));
    }

    [Fact]
    public void A_cell_entirely_outside_the_target_yields_nothing()
    {
        Assert.Empty(Walk(Cell2x2(), 5, 5, 2, 2));
        Assert.Empty(Walk(Cell2x2(), -5, -5, 2, 2));
    }

    [Fact]
    public void Mirroring_a_clipped_cell_mirrors_before_clipping()
    {
        // Dropping the left column drops the pixel drawn there, which mirroring made the cell's right one.
        Assert.Equal([(0, 1), (2, 3)], Walk(Cell2x2(), -1, 0, 2, 2, mirrored: true));
    }

    [Fact]
    public void Transparency_is_left_to_the_caller_so_index_zero_is_still_yielded()
    {
        var cell = new SpriteCell(0, 0, 2, 1, [0, 7]);
        Assert.Equal([(0, 0), (1, 7)], Walk(cell, 0, 0, 2, 1));
    }
}
