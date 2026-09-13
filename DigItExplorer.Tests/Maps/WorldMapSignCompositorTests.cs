using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="WorldMapSignCompositor"/>'s cell geometry and per-world sheets.</summary>
public class WorldMapSignCompositorTests
{
    private static ResourceLibrary OpenLibrary()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        return ResourceLibrary.Open(gameDir);
    }

    private static readonly World[] SignWorlds = [World.Caves, World.Water, World.Snow, World.Underworld];

    private static readonly SignType[] AllTypes =
        [SignType.Level, SignType.Checkpoint, SignType.Trace, SignType.Gate, SignType.Draggo];

    /// <summary>Verifies that every signpost type composes to a uniform 28x27 graphic-only still per world.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Every_type_composes_to_a_uniform_28x27_still()
    {
        using var library = OpenLibrary();

        int worlds = 0;
        foreach (var world in SignWorlds)
        {
            if (!library.Contains(WorldMapSignCompositor.Sheet(world))) continue;
            worlds++;

            foreach (var type in AllTypes)
            {
                var result = WorldMapSignCompositor.Compose(library.TryRead, world, type);
                Assert.NotNull(result);
                Assert.Equal((28, 27), (result!.Width, result.Height));
                Assert.True(result.ShowedGraphic);
                Assert.False(result.ShowedCollision);
                Assert.Contains<byte>(255, result.Alpha); // the sign itself
                Assert.Contains<byte>(0, result.Alpha);   // index 0 and the separator keyed out
            }
        }

        Assert.Equal(4, worlds);
    }

    /// <summary>Verifies that the four in-game signpost types compose to pictures of their own.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void The_four_used_types_have_distinct_pictures()
    {
        using var library = OpenLibrary();

        var byType = new Dictionary<SignType, byte[]>();
        foreach (var type in (SignType[])[SignType.Level, SignType.Checkpoint, SignType.Trace, SignType.Gate])
        {
            var result = WorldMapSignCompositor.Compose(library.TryRead, World.Caves, type);
            Assert.NotNull(result);
            byType[type] = result!.Rgb;
        }

        foreach (var (a, first) in byType)
        foreach (var (b, second) in byType)
        {
            if (a >= b) continue;
            Assert.False(first.SequenceEqual(second), $"{a} and {b} compose to the same picture");
        }
    }

    /// <summary>Verifies that each world draws the same signpost type through its own sheet.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Each_world_draws_the_level_sign_through_its_own_sheet()
    {
        using var library = OpenLibrary();

        var caves = WorldMapSignCompositor.Compose(library.TryRead, World.Caves, SignType.Level);
        var water = WorldMapSignCompositor.Compose(library.TryRead, World.Water, SignType.Level);
        Assert.NotNull(caves);
        Assert.NotNull(water);
        Assert.NotEqual(caves!.Rgb, water!.Rgb);
    }
}
