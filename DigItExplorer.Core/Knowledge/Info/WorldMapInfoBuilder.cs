using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for world map nodes.</summary>
public static class WorldMapInfoBuilder
{
    /// <summary>Constructs Identity, Source, and Contents info sections for a world map document.</summary>
    public static NodeInfo Build(WorldMapDocument map, ResourceLibrary library, NodeTableData? nodes,
        WorldMapMusic? music)
    {
        var prefix = GameKnowledge.WorldMapPrefix(map.World);

        var identity = InfoSections.Of("Identity",
            InfoRow.OrNull("World", nodes?.WorldName(map.World)),
            new InfoRow("Files", $"{prefix}*"));

        var source = InfoSource.Of(library, InfoSource.NamesStartingWith(library, prefix));

        var contentRows = new List<InfoRow?>
        {
            new InfoRow("Canvas", $"{map.Width}×{map.Height} px"),
            new InfoRow("Layers", Layers(map)),
            InfoRow.OrNull("Path nodes", map.Path?.Nodes.Count.ToString()),
        };
        contentRows.Add(music?.TuneFile(map.World) is { } tune
            ? new InfoRow("Music", tune, ValueFile: library.Contains(tune) ? tune : null)
            : null);
        contentRows.AddRange(SignRows(map, nodes).Select(r => (InfoRow?)r));
        var contents = InfoSections.Of("Contents", [.. contentRows]);

        return NodeInfos.Of(null, identity, source, contents);
    }

    private static string Layers(WorldMapDocument map)
    {
        var present = new List<string>();
        if (map.Sky is not null) present.Add("Sky");
        if (map.Background is not null) present.Add("Background");
        present.Add("Front");
        if (map.Path is not null) present.Add("Path");
        if (map.SignSheet is not null) present.Add("Signs");
        return string.Join(" + ", present);
    }

    private static IEnumerable<InfoRow> SignRows(WorldMapDocument map, NodeTableData? nodes)
        => nodes is null
            ? []
            : nodes.Nodes(map.World)
                   .GroupBy(n => n.Sign)
                   .OrderBy(g => g.Key)
                   .Select(g => new InfoRow(g.Key.ToString(), g.Count().ToString()));
}
