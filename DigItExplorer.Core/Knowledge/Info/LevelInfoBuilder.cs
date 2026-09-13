using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for level nodes from map data and resource library files.</summary>
public static class LevelInfoBuilder
{
    private static readonly string[] BackingFileSuffixes = ["F.MPF", "M.MPF", ".DLF", ".PAL"];

    /// <summary>Constructs Identity, Source, and Contents info sections for a playable level.</summary>
    public static NodeInfo Build(string stem, MapDocument map, ResourceLibrary library,
        NodeTableData? nodes, EntityNames? entityNames)
    {
        bool parsed = GameKnowledge.TryParseLevelFile(stem, out var world, out int nodeIndex, out _);
        var node = parsed ? nodes?.Node(world, nodeIndex) : null;

        var identity = InfoSections.Of("Identity",
            InfoRow.OrNull("World", parsed ? nodes?.WorldName(world) : null),
            InfoRow.OrNull("Node", parsed ? nodeIndex.ToString() : null),
            InfoRow.OrNull("Sign", node?.Sign.ToString()),
            new InfoRow("Name", node?.Name ?? stem));

        var sourceRows = InfoSource.Rows(library, BackingFileSuffixes.Select(s => stem + s))
            .Select(r => (InfoRow?)r).ToList();
        if (map.RealPalette is null)
            sourceRows.Add(new InfoRow("Palette", "Fallback (no .PAL in this copy of the game)"));
        var source = InfoSections.Of("Source", [.. sourceRows]);

        var contentRows = new List<InfoRow?>
        {
            new InfoRow("Terrain", $"{map.Terrain.Cols}×{map.Terrain.Rows} tiles · {map.Terrain.Width}×{map.Terrain.Height} px"),
            new InfoRow("Collision", map.Collision is not null ? "Present" : "Not present"),
        };
        contentRows.Add(map.TuneFile is { } tune
            ? new InfoRow("Music", tune, ValueFile: library.Contains(tune) ? tune : null)
            : null);
        contentRows.AddRange(EntityCensus.Rows(map.Entities, entityNames).Select(r => (InfoRow?)r));
        var contents = InfoSections.Of("Contents", [.. contentRows]);

        var damage = DamageNote.ForLayers(
            ("Terrain", map.Terrain.MissingBlocks, map.Terrain.BlockCount),
            ("Collision", map.Collision?.MissingBlocks ?? 0, map.Collision?.BlockCount ?? 0));

        return NodeInfos.Of(null, identity, source, contents) with { Damage = damage };
    }
}
