using DigItExplorer.App.Models;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.App.Services.ResourceTree;

/// <summary>Constructs the Worlds navigation tree branch representing levels and world-bound screens.</summary>
internal static class WorldsBranch
{
    /// <summary>Builds the Worlds branch grouping levels and substages under world nodes.</summary>
    public static TreeNode? Build(IEnumerable<string> names, ResourceLibrary library, GameData data)
    {
        var byWorldNode = new Dictionary<World, SortedDictionary<int, SortedSet<string>>>();
        var present = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

        foreach (var name in present)
        {
            if (!name.EndsWith("F.MPF", StringComparison.OrdinalIgnoreCase)) continue;
            var stem = GameKnowledge.LevelStem(name);
            if (stem is null || !GameKnowledge.TryParseLevelFile(stem, out var world, out var node, out _)) continue;

            if (!byWorldNode.TryGetValue(world, out var nodes))
                byWorldNode[world] = nodes = new SortedDictionary<int, SortedSet<string>>();
            if (!nodes.TryGetValue(node, out var stems))
                nodes[node] = stems = new SortedSet<string>(StringComparer.Ordinal);
            stems.Add(stem);
        }

        if (byWorldNode.Count == 0) return null;

        var root = new TreeNode { Label = "Worlds", IsExpanded = true };

        // Boss arena (LVL750) mapped under Spurkasaur Lair.
        SortedSet<string>? bossArenaStems = null;
        const int BossArenaNode = 15;

        foreach (var world in GameKnowledge.Worlds)
        {
            bool hasLevels = byWorldNode.TryGetValue(world, out var nodes);

            if (world == World.Underworld && hasLevels && nodes!.TryGetValue(BossArenaNode, out var arenaStems))
            {
                bossArenaStems = arenaStems;
                nodes.Remove(BossArenaNode);
                hasLevels = nodes.Count > 0;
            }

            bool hasWorldMap = library.Names.Contains($"{GameKnowledge.WorldMapPrefix(world)}LD.MPF", StringComparer.OrdinalIgnoreCase);
            bool hasBossArena = world == World.Boss && bossArenaStems is not null;

            var specialNodes = data.Nodes.Nodes(world)
                .Where(n => n.Sign is SignType.Checkpoint or SignType.Trace)
                .ToDictionary(n => n.Index);

            if (!hasLevels && !hasWorldMap && !hasBossArena && specialNodes.Count == 0) continue;

            var worldName = data.Nodes.WorldName(world);
            var worldNode = new TreeNode { Label = worldName };

            if (hasWorldMap)
                worldNode.Children.Add(new TreeNode
                {
                    Label = "World Map", WorldMap = world, InfoPath = [worldName, "World Map"],
                });

            if (hasBossArena)
            {
                var stem = bossArenaStems!.Min!;
                var arenaName = data.Nodes.Node(World.Underworld, BossArenaNode)?.Name;
                worldNode.Children.Add(new TreeNode
                {
                    Label = stem, LevelStem = stem, InfoPath = [worldName, arenaName ?? "", stem],
                });
            }

            var levelNodes = hasLevels ? nodes! : new SortedDictionary<int, SortedSet<string>>();
            foreach (var nodeIndex in levelNodes.Keys.Concat(specialNodes.Keys).Distinct().OrderBy(i => i))
            {
                if (specialNodes.TryGetValue(nodeIndex, out var special))
                {
                    var leaf = SpecialScreenLeaf(world, worldName, special, present);
                    if (leaf is not null) worldNode.Children.Add(leaf);
                    continue;
                }

                var stems = levelNodes[nodeIndex];
                var name = data.Nodes.Node(world, nodeIndex)?.Name ?? $"Node {nodeIndex}";
                var stemList = stems.ToList();
                string prefix = stemList[0]!.Substring(0, 5);
                var bonusSlots = BonusZones.GetBonusSlots(prefix, stems, library.TryRead);

                string StemField(string stem) => bonusSlots.Contains(stem[5] - '0') ? $"{stem} (bonus)" : stem;

                if (stems.Count == 1)
                {
                    // Single substage: the level node itself is the leaf (no redundant single child).
                    var stem = stems.Min!;
                    var stemField = StemField(stem);
                    var label = stemField.EndsWith("(bonus)", StringComparison.Ordinal) ? $"{name} (bonus)" : name;
                    worldNode.Children.Add(new TreeNode
                    {
                        Label = label, LevelStem = stem, InfoPath = [worldName, name, stemField],
                    });
                }
                else
                {
                    var levelNode = new TreeNode { Label = $"{name} ({stems.Count})" };
                    foreach (var stem in stems)
                    {
                        var stemField = StemField(stem!);
                        levelNode.Children.Add(new TreeNode
                        {
                            Label = stemField, LevelStem = stem, InfoPath = [worldName, name, stemField],
                        });
                    }
                    worldNode.Children.Add(levelNode);
                }
            }
            root.Children.Add(worldNode);
        }
        return root;
    }

    /// <summary>Builds leaf nodes for world-bound special screens such as Checkpoints or Traces of Dugette.</summary>
    public static TreeNode? SpecialScreenLeaf(World world, string worldName, LevelNode special, HashSet<string> present)
    {
        if (special.Sign == SignType.Checkpoint)
        {
            var save = $"SAVE0{(int)world}.MPF";
            return present.Contains(save)
                ? new TreeNode { Label = special.Name, Resource = save, InfoPath = [worldName, special.Name], UseScreensZoom = true }
                : null;
        }

        var scrn = $"R0{(int)world + 1}_SCRN.SPF";
        var stry = $"R0{(int)world + 1}_STRY.TXT";
        bool hasScrn = present.Contains(scrn), hasStry = present.Contains(stry);
        var node = new TreeNode { Label = special.Name };
        string[] Path(string piece) => [worldName, special.Name, piece];
        if (hasScrn) node.Children.Add(new TreeNode { Label = "Background", Resource = scrn, InfoPath = Path("Background"), UseScreensZoom = true });
        if (hasStry) node.Children.Add(new TreeNode { Label = "Story Text", Resource = stry, InfoPath = Path("Story Text") });
        if (hasScrn && hasStry)
            node.Children.Add(new TreeNode { Label = "Reconstruction", StoryScene = world, InfoPath = Path("Reconstruction") });
        return node.Children.Count > 0 ? node : null;
    }
}
