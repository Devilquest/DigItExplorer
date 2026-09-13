using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for the water-world exit sign.</summary>
public static class ExitSignInfoBuilder
{
    /// <summary>Constructs Identity, Source, and Contents info sections for the water-world exit sign.</summary>
    public static NodeInfo Build((int Width, int Height)? cellSize, ResourceLibrary library, NodeTableData? nodes)
    {
        var identity = InfoSections.Of("Identity",
            InfoRow.OrNull("World", nodes?.WorldName(World.Water)));

        var source = InfoSource.Section(library, ExitSignCompositor.Sheet, ExitSignCompositor.Palette);

        var contents = InfoSections.Of("Contents",
            InfoRow.OrNull("Cell size", cellSize is { } size ? $"{size.Width}×{size.Height} px" : null),
            new InfoRow("Planes", "Graphic"));

        return NodeInfos.Of(null, identity, source, contents);
    }
}
