using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="DigSpotCompositor"/>'s cell choice.</summary>
public class DigSpotCompositorTests
{
    private static ResourceLibrary OpenLibrary()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        return ResourceLibrary.Open(gameDir);
    }

    /// <summary>Verifies that each dig spot state composes to a distinct pixel bitmap.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Each_state_composes_to_a_picture_of_its_own()
    {
        using var library = OpenLibrary();

        var composed = new Dictionary<DigSpotState, SpriteRenderResult>();
        foreach (var state in (DigSpotState[])[DigSpotState.Exit, DigSpotState.Bonus, DigSpotState.BonusSealed])
        {
            var result = DigSpotCompositor.Compose(library.TryRead, state);
            Assert.NotNull(result);
            composed[state] = result!;
        }

        foreach (var (a, first) in composed)
        foreach (var (b, second) in composed)
        {
            if (a >= b) continue;
            Assert.False(first.Alpha.SequenceEqual(second.Alpha), $"{a} and {b} compose to the same shape");
        }
    }

    /// <summary>Verifies that the underworld exit shares the exit alpha mask with distinct palette colors.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void The_underworlds_exit_is_the_ordinary_exit_recolored()
    {
        using var library = OpenLibrary();

        var exit = DigSpotCompositor.Compose(library.TryRead, DigSpotState.Exit);
        var underworld = DigSpotCompositor.Compose(library.TryRead, DigSpotState.ExitUnderworld);
        var bonus = DigSpotCompositor.Compose(library.TryRead, DigSpotState.Bonus);
        Assert.NotNull(exit);
        Assert.NotNull(underworld);
        Assert.NotNull(bonus);

        Assert.Equal(exit!.Alpha, underworld!.Alpha); // the same silhouette, pixel for pixel
        Assert.NotEqual(exit.Rgb, underworld.Rgb);
        Assert.NotEqual(exit.Alpha, bonus!.Alpha);
    }
}
