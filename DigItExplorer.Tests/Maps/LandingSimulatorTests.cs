using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="LandingSimulator"/>'s player/boss landing-spot fall simulation.</summary>
public class LandingSimulatorTests
{
    /// <summary>Verifies player and boss drop simulation coordinates across sample levels.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Landings_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var collision000 = ComposeCollision(library,"LVL000");
        Assert.Equal((40, 99), LandingSimulator.PlayerLanding(collision000));

        var collision750 = ComposeCollision(library,"LVL750");
        Assert.Equal((40, 142), LandingSimulator.PlayerLanding(collision750));
        Assert.Equal((240, 108), LandingSimulator.BossLanding(collision750));
    }

    /// <summary>Verifies that spawn synthesis appends the player spawn and relocates the boss in LVL750.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void SynthesizeSpawns_adds_player_and_relocates_boss()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var dlfBytes = library.Read("LVL750.DLF");
        var records = DlfRecord.ReadAll(dlfBytes);
        var collision = ComposeCollision(library,"LVL750");

        var synthesized = LandingSimulator.SynthesizeSpawns(records, collision, "LVL750");

        Assert.Equal(records.Count + 1, synthesized.Count);
        var player = Assert.Single(synthesized, r => r.Category == LandingSimulator.CatPlayer);
        Assert.Equal((40, 142), (player.X, player.Y));
        Assert.Equal(1, player.P0);

        var boss = Assert.Single(synthesized, r => r.Category == 0x32);
        Assert.Equal((240, 108), (boss.X, boss.Y));
        Assert.Equal(0xFFFF, boss.P0);
    }

    private static CollisionImage ComposeCollision(ResourceLibrary library, string level)
    {
        var mMpf = library.Read($"{level}M.MPF");
        var dlf = library.Read($"{level}.DLF");
        return CollisionCompositor.Compose(mMpf, dlf);
    }
}
