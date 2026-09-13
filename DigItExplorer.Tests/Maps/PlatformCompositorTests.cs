using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies platform graphic and collider plane slicing and composition in <see cref="PlatformCompositor"/>.</summary>
public class PlatformCompositorTests
{
    // The worlds that ship a platform sheet. Water is deliberately absent: it ships no WO_MVL file at all.
    private static readonly World[] PlatformWorlds = [World.Caves, World.Snow, World.Underworld];

    private static ResourceLibrary OpenLibrary()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        return ResourceLibrary.Open(gameDir);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Every_platform_world_ships_two_cells_and_the_water_world_none()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        foreach (var world in PlatformWorlds)
            Assert.Equal(2, PlatformCompositor.CellCount(load, world));

        Assert.Equal(0, PlatformCompositor.CellCount(load, World.Water));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Both_cells_compose_at_half_their_packed_height()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        // The two cells the sheet packs, as (width, packed height): each is a graphic half stacked on a
        // collider half of equal height, so the composed still is half as tall as the cell.
        (int W, int H)[] packed = [(68, 76), (54, 120)];

        foreach (var world in PlatformWorlds)
        {
            for (int cell = 0; cell < packed.Length; cell++)
            {
                var result = PlatformCompositor.Compose(load, world, cell, new SpriteRenderOptions(true, true));
                Assert.NotNull(result);
                Assert.Equal(packed[cell].W, result!.Width);
                Assert.Equal(packed[cell].H / 2, result.Height);
                Assert.Equal(result.Width * result.Height * 3, result.Rgb.Length);
                Assert.Equal(result.Width * result.Height, result.Alpha.Length);
            }
        }
    }

    /// <summary>Verifies that platform graphic planes preserve transparent cell padding.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void The_graphic_plane_is_covered_exactly_where_the_cell_is_drawn()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        foreach (var world in PlatformWorlds)
        {
            for (int cell = 0; cell < 2; cell++)
            {
                var (graphic, _) = Halves(load, world, cell);
                var result = PlatformCompositor.Compose(load, world, cell, new SpriteRenderOptions(true, false))!;

                Assert.Equal(graphic.Pixels.Select(p => p == 0 ? (byte)0 : (byte)255).ToArray(), result.Alpha);
                Assert.Contains((byte)0, result.Alpha);
            }
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Neither_plane_requested_composes_nothing()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        Assert.Null(PlatformCompositor.Compose(load, World.Caves, 0, new SpriteRenderOptions(false, false)));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void A_cell_the_sheet_does_not_carry_composes_nothing()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        // Past the last cell of a sheet that is there, then a world whose sheet is not: without the first
        // count, an install missing WO_MVL00 would answer nothing here and read as a pass.
        Assert.Equal(2, PlatformCompositor.CellCount(load, World.Caves));
        Assert.Null(PlatformCompositor.Compose(load, World.Caves, 2, new SpriteRenderOptions(true, true)));
        Assert.Null(PlatformCompositor.Compose(load, World.Water, 0, new SpriteRenderOptions(true, true)));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void The_two_planes_are_different_pictures()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        foreach (var world in PlatformWorlds)
        {
            for (int cell = 0; cell < 2; cell++)
            {
                var graphic = PlatformCompositor.Compose(load, world, cell, new SpriteRenderOptions(true, false))!;
                var collision = PlatformCompositor.Compose(load, world, cell, new SpriteRenderOptions(false, true))!;
                Assert.False(graphic.Rgb.AsSpan().SequenceEqual(collision.Rgb),
                    $"{world} cell {cell}: Graphic and Collision composed identical pixels");
            }
        }
    }

    /// <summary>Verifies that platform collider planes map material codes to standard overlay colors.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void The_collider_alone_reads_its_codes_the_way_a_level_reads_them()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        foreach (var world in PlatformWorlds)
        {
            for (int cell = 0; cell < 2; cell++)
            {
                var codes = ColliderCodes(load, world, cell);
                var result = PlatformCompositor.Compose(load, world, cell, new SpriteRenderOptions(false, true))!;

                Assert.Equal<byte>([0, 15, 64, 96], codes.Distinct().Order().ToArray());

                for (int i = 0; i < codes.Length; i++)
                {
                    var (r, g, b, a) = MaterialPalette.OverlayColorOf(codes[i]);
                    Assert.Equal(a, result.Alpha[i]);
                    if (a == 0) continue;
                    Assert.Equal((r, g, b), (result.Rgb[i * 3], result.Rgb[i * 3 + 1], result.Rgb[i * 3 + 2]));
                }
            }
        }
    }

    /// <summary>Verifies that background codes correspond exactly to sprite padding and solid body pixels.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void The_two_background_codes_are_exactly_the_padding_and_the_body()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        foreach (var world in PlatformWorlds)
        {
            for (int cell = 0; cell < 2; cell++)
            {
                var (graphic, collider) = Halves(load, world, cell);
                for (int i = 0; i < collider.Pixels.Length; i++)
                {
                    bool drawn = graphic.Pixels[i] != 0;
                    if (collider.Pixels[i] == 15) Assert.False(drawn, $"{world} cell {cell}: open void at px {i} is drawn");
                    if (collider.Pixels[i] == 0) Assert.True(drawn, $"{world} cell {cell}: solid mass at px {i} is not drawn");
                }
            }
        }
    }

    /// <summary>Guards that collider walkable surface bounds fall entirely within drawn platform art bounds.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void The_collider_surface_lands_inside_the_drawn_platform()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        foreach (var world in PlatformWorlds)
        {
            for (int cell = 0; cell < 2; cell++)
            {
                var (graphicCell, colliderCell) = Halves(load, world, cell);
                var art = BoundsOf(graphicCell, p => p != 0);
                var surface = BoundsOf(colliderCell, p => p == 64);

                Assert.True(surface.X0 >= art.X0 && surface.X1 <= art.X1,
                    $"{world} cell {cell}: collider surface x {surface.X0}..{surface.X1} outside art x {art.X0}..{art.X1}");
                Assert.True(surface.Y0 >= art.Y0 && surface.Y1 <= art.Y1,
                    $"{world} cell {cell}: collider surface y {surface.Y0}..{surface.Y1} outside art y {art.Y0}..{art.Y1}");
            }
        }
    }

    /// <summary>Verifies that collider templates remain byte-identical across all world reskins.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Every_world_ships_the_same_collider_template()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        for (int cell = 0; cell < 2; cell++)
        {
            var reference = ColliderCodes(load, World.Caves, cell);
            foreach (var world in PlatformWorlds)
                Assert.Equal(reference, ColliderCodes(load, world, cell));
        }
    }

    private static (SpriteCell Graphic, SpriteCell Collider) Halves(Func<string, byte[]?> load, World world, int cell)
    {
        var bytes = load($"WO_MVL0{(int)world}.SPF")!;
        var cells = SpriteSheetSlicer.Slice(SheetImage.Read(bytes).Frames[0], 255);
        return (SpriteSheetSlicer.TopHalf(cells[cell]), SpriteSheetSlicer.BottomHalf(cells[cell]));
    }

    private static byte[] ColliderCodes(Func<string, byte[]?> load, World world, int cell)
        => Halves(load, world, cell).Collider.Pixels;

    private static (int X0, int Y0, int X1, int Y1) BoundsOf(SpriteCell cell, Func<byte, bool> hit)
    {
        int x0 = cell.W, y0 = cell.H, x1 = -1, y1 = -1;
        for (int y = 0; y < cell.H; y++)
        for (int x = 0; x < cell.W; x++)
        {
            if (!hit(cell.Pixels[y * cell.W + x])) continue;
            if (x < x0) x0 = x;
            if (x > x1) x1 = x;
            if (y < y0) y0 = y;
            if (y > y1) y1 = y;
        }
        return (x0, y0, x1, y1);
    }
}
