using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies drain graphic and collision plane composition in <see cref="DrainCompositor"/>.</summary>
public class DrainCompositorTests
{
    private static readonly DrainState[] States = [DrainState.Open, DrainState.Sealed];

    private static ResourceLibrary OpenLibrary()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        return ResourceLibrary.Open(gameDir);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Both_states_compose_at_the_grids_cell_size()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        foreach (var state in States)
        {
            var result = DrainCompositor.Compose(load, state, new SpriteRenderOptions(true, true));
            Assert.NotNull(result);
            Assert.Equal(89, result!.Width);
            Assert.Equal(50, result.Height);
            Assert.Equal(result.Width * result.Height * 3, result.Rgb.Length);
            Assert.Equal(result.Width * result.Height, result.Alpha.Length);
        }
    }

    /// <summary>Verifies that drain graphic planes preserve transparent cell padding.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void The_graphic_plane_leaves_the_cells_padding_uncovered()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        foreach (var state in States)
        {
            var result = DrainCompositor.Compose(load, state, new SpriteRenderOptions(true, false))!;
            int covered = result.Alpha.Count(a => a == 255);
            Assert.All(result.Alpha, a => Assert.True(a is 0 or 255, $"{state}: partly covered pixel ({a})"));
            Assert.InRange(covered, 1, result.Alpha.Length - 1);
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Neither_plane_requested_composes_nothing()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        Assert.Null(DrainCompositor.Compose(load, DrainState.Open, new SpriteRenderOptions(false, false)));
    }

    [Fact]
    public void An_install_without_the_sheet_composes_nothing()
    {
        Assert.Null(DrainCompositor.Compose(_ => null, DrainState.Open, new SpriteRenderOptions(true, true)));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void The_two_planes_are_different_pictures()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        foreach (var state in States)
        {
            var graphic = DrainCompositor.Compose(load, state, new SpriteRenderOptions(true, false))!;
            var collision = DrainCompositor.Compose(load, state, new SpriteRenderOptions(false, true))!;
            Assert.False(graphic.Rgb.AsSpan().SequenceEqual(collision.Rgb),
                $"{state}: Graphic and Collision composed identical pixels");
        }
    }

    /// <summary>Verifies that open and sealed drain states produce distinct pixel renders on both planes.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void The_two_states_differ_on_both_planes()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        foreach (var options in new[] { new SpriteRenderOptions(true, false), new SpriteRenderOptions(false, true) })
        {
            var open = DrainCompositor.Compose(load, DrainState.Open, options)!;
            var sealedDrain = DrainCompositor.Compose(load, DrainState.Sealed, options)!;
            Assert.False(open.Rgb.AsSpan().SequenceEqual(sealedDrain.Rgb));
        }
    }

    /// <summary>Verifies that drain collision planes render solid surface material code 64.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void The_collision_plane_alone_shows_only_the_solid_surface()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        var (sr, sg, sb, sa) = MaterialPalette.OverlayColorOf(64);

        foreach (var state in States)
        {
            var result = DrainCompositor.Compose(load, state, new SpriteRenderOptions(false, true))!;
            int surface = 0;
            for (int p = 0; p < result.Alpha.Length; p++)
            {
                if (result.Alpha[p] == 0) continue;
                int i = p * 3;
                Assert.Equal((sr, sg, sb, sa),
                    (result.Rgb[i], result.Rgb[i + 1], result.Rgb[i + 2], result.Alpha[p]));
                surface++;
            }
            Assert.True(surface > 0, $"{state}: expected the solid surface to be drawn somewhere");
        }
    }

    /// <summary>Verifies that the open drain state contains non-solid passage rows absent from the sealed state.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Only_the_open_state_has_a_way_through_it()
    {
        using var library = OpenLibrary();
        var load = library.TryRead;

        int openRows = RowsWithoutCollision(load, DrainState.Open);
        int sealedRows = RowsWithoutCollision(load, DrainState.Sealed);

        // Both share the four blank rows of the sprite's own border, two at the top and two at the bottom.
        Assert.Equal(4, sealedRows);
        Assert.True(openRows > sealedRows, $"expected the open entrance to have a passage: {openRows} vs {sealedRows}");
    }

    private static int RowsWithoutCollision(Func<string, byte[]?> load, DrainState state)
    {
        var result = DrainCompositor.Compose(load, state, new SpriteRenderOptions(false, true))!;

        int rows = 0;
        for (int y = 0; y < result.Height; y++)
        {
            bool any = false;
            for (int x = 0; x < result.Width && !any; x++)
                any = result.Alpha[y * result.Width + x] != 0;
            if (!any) rows++;
        }
        return rows;
    }
}
