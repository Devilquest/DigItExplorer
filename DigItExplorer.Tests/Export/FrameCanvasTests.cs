using DigItExplorer.Core.Export;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.Tests.Export;

/// <summary>Verifies stage centering, margin distribution, and transparency folding in <see cref="FrameCanvas"/>.</summary>
public class FrameCanvasTests
{
    private static SpriteCell Cell(int w, int h, params byte[] pixels) => new(0, 0, w, h, pixels);

    [Fact]
    public void A_cell_the_size_of_the_stage_fills_it_unchanged()
    {
        var placed = FrameCanvas.Place(Cell(2, 2, 1, 2, 3, 4), 2, 2);

        Assert.Equal(2, placed.Width);
        Assert.Equal(2, placed.Height);
        Assert.Equal([1, 2, 3, 4], placed.Indices);
    }

    [Fact]
    public void A_smaller_cell_is_centered_on_transparency()
    {
        // A 2×2 cell on a 4×4 stage lands at (1,1).
        var placed = FrameCanvas.Place(Cell(2, 2, 1, 2, 3, 4), 4, 4);

        Assert.Equal(
        [
            0, 0, 0, 0,
            0, 1, 2, 0,
            0, 3, 4, 0,
            0, 0, 0, 0,
        ], placed.Indices);
    }

    /// <summary>Verifies that uneven stage margins distribute deterministically across frames.</summary>
    [Fact]
    public void An_uneven_margin_leans_the_same_way_every_time()
    {
        var placed = FrameCanvas.Place(Cell(2, 1, 1, 2), 5, 1);

        Assert.Equal([0, 1, 2, 0, 0], placed.Indices);
    }

    [Fact]
    public void A_cell_larger_than_the_stage_keeps_its_own_size()
    {
        var placed = FrameCanvas.Place(Cell(3, 1, 1, 2, 3), stageWidth: 2, stageHeight: 1);

        Assert.Equal(3, placed.Width);
        Assert.Equal([1, 2, 3], placed.Indices);
    }

    /// <summary>Verifies that multi-index transparency keys map to index zero on output canvases.</summary>
    [Fact]
    public void Every_transparent_index_the_sheet_uses_becomes_zero()
    {
        var alsoTransparent = new HashSet<byte> { 51, 171 };
        var placed = FrameCanvas.Place(Cell(4, 1, 51, 9, 171, 0), 4, 1, alsoTransparent);

        Assert.Equal([0, 9, 0, 0], placed.Indices);
    }

    [Fact]
    public void Without_extra_transparent_indices_only_zero_is_dropped()
    {
        var placed = FrameCanvas.Place(Cell(3, 1, 51, 0, 9), 3, 1);

        Assert.Equal([51, 0, 9], placed.Indices);
    }
}
