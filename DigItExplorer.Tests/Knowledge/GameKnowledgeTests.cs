using System.Buffers.Binary;
using System.Text;
using DigItExplorer.Core.Archives;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="GameKnowledge"/> against the game's own compiled node-info table and level archive.</summary>
public class GameKnowledgeTests
{
    /// <summary>Build fingerprint test verifying decoded level names, sign types, and gate targets.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void NodeTable_read_from_the_executable_matches_the_known_build()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));
        var nodes = data.Nodes;

        Assert.Equal(45, GameKnowledge.Worlds.Sum(w => nodes.Nodes(w).Count));
        Assert.Equal([11, 10, 13, 10, 1], GameKnowledge.Worlds.Select(w => nodes.Nodes(w).Count));

        Assert.Equal("Warm Up Run", nodes.Node(World.Caves, 0)?.Name);
        Assert.Equal("Snowball Blitz", nodes.Node(World.Snow, 11)?.Name);
        Assert.Equal("Spurkasaur Lair", nodes.Node(World.Underworld, 15)?.Name);

        // The repeated names are the game's own: every checkpoint and lore node shares one label.
        Assert.Equal("Check Point", nodes.Node(World.Caves, 5)?.Name);
        Assert.Equal(SignType.Checkpoint, nodes.Node(World.Caves, 5)?.Sign);
        Assert.Equal("Traces of Dugette", nodes.Node(World.Caves, 6)?.Name);
        Assert.Equal(SignType.Trace, nodes.Node(World.Caves, 6)?.Sign);

        var gate = nodes.Node(World.Caves, 10);
        Assert.Equal(SignType.Gate, gate?.Sign);
        Assert.Equal(new GateTarget(World.Water, 0), gate?.Gate);
    }

    /// <summary>Verifies that all world names are derived from their corresponding gate labels.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_world_name_comes_from_a_gate_label()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        Assert.Equal("Dry Lands", data.Nodes.WorldName(World.Caves));
        Assert.Equal("Great Waters", data.Nodes.WorldName(World.Water));
        Assert.Equal("Frozen North", data.Nodes.WorldName(World.Snow));
        Assert.Equal("Underworld", data.Nodes.WorldName(World.Underworld));
        Assert.Equal("Spurkasaur Lair", data.Nodes.WorldName(World.Boss));

        Assert.Equal(new GateTarget(World.Underworld, 0), data.Nodes.Node(World.Snow, 12)?.Gate);
    }

    /// <summary>Guards per-world level stem counts (42 Caves, 23 Water, 44 Snow, 16 Underworld, 1 Menu = 126 total).</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Level_files_group_into_the_documented_per_world_counts()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        var stemsByWorld = new Dictionary<World, HashSet<string>>();
        var menuStems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var archiveName in new[] { "DIGIT0.XRS", "DIGIT1.XRS", "DIGIT2.XRS", "DIGIT3.XRS", "DIGIT4.XRS", "DIGITX.XRS" })
        {
            var path = Path.Combine(gameDir, archiveName);
            if (!File.Exists(path)) continue;
            using var archive = XrsArchive.Open(path);
            foreach (var entry in archive.Entries)
            {
                if (GameKnowledge.IsMenuFile(entry.Name))
                {
                    menuStems.Add(GameKnowledge.LevelStem(entry.Name)!);
                }
                else if (GameKnowledge.TryParseLevelFile(entry.Name, out var world, out _, out _))
                {
                    (stemsByWorld.TryGetValue(world, out var set) ? set : stemsByWorld[world] = new(StringComparer.OrdinalIgnoreCase))
                        .Add(GameKnowledge.LevelStem(entry.Name)!);
                }
            }
        }

        Assert.Equal(42, stemsByWorld[World.Caves].Count);
        Assert.Equal(23, stemsByWorld[World.Water].Count);
        Assert.Equal(44, stemsByWorld[World.Snow].Count);
        Assert.Equal(16, stemsByWorld[World.Underworld].Count);
        Assert.Single(menuStems);

        int total = stemsByWorld.Values.Sum(s => s.Count) + menuStems.Count;
        Assert.Equal(126, total);
    }
}
