using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Tests;

/// <summary>Guards the per-world tune names read from the level-select screen's music branch.</summary>
public class WorldMapMusicTests
{
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_world_resolves_to_a_tune_file_the_install_actually_ships()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        foreach (var world in GameKnowledge.Worlds)
        {
            var file = data.WorldMapMusic.TuneFile(world);
            Assert.StartsWith("TUNE", data.WorldMapMusic.TuneName(world));
            Assert.True(library.Contains(file), $"{world} selects {file}, which is not in this install");
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void The_three_screens_that_share_a_tune_are_the_three_the_branch_never_tests_for()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        // The branch has one arm per special-cased world and one fall-through, so however many worlds the
        // game grows, the untested ones can only ever end up on the same tune.
        var music = data.WorldMapMusic;
        var distinct = GameKnowledge.Worlds.Select(music.TuneName).Distinct().Count();
        Assert.Equal(ExeLayout.WorldMapTuneCaseCount + 1, distinct);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void The_branch_reads_the_tunes_this_build_ships() // build fingerprint, not a guard
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        var music = data.WorldMapMusic;
        Assert.Equal("TUNE1", music.TuneName(World.Caves));
        Assert.Equal("TUNE1", music.TuneName(World.Water));
        Assert.Equal("TUNE1", music.TuneName(World.Snow));
        Assert.Equal("TUNE12", music.TuneName(World.Underworld));
        Assert.Equal("TUNE5", music.TuneName(World.Boss));
    }
}
