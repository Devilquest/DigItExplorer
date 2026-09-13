using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for a world-map signpost.</summary>
public static class WorldMapSignInfoBuilder
{
    /// <summary>Constructs Identity, Source, and Contents info sections for a signpost type in a world.</summary>
    public static NodeInfo Build(World world, SignType type, (int Width, int Height)? cellSize,
        ResourceLibrary library, NodeTableData? nodes)
    {
        var identity = InfoSections.Of("Identity",
            InfoRow.OrNull("World", nodes?.WorldName(world)),
            new InfoRow("Type", SignTypeNames.Label(type)));

        var source = InfoSource.Section(library,
            WorldMapSignCompositor.Sheet(world), WorldMapSignCompositor.Palette(world));

        var contents = InfoSections.Of("Contents",
            InfoRow.OrNull("Cell size", cellSize is { } size ? $"{size.Width}×{size.Height} px" : null),
            new InfoRow("Planes", "Graphic"));

        return NodeInfos.Of(null, identity, source, contents);
    }
}
