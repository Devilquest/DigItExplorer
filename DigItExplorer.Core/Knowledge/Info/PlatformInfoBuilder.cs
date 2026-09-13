using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for moving platform cells.</summary>
public static class PlatformInfoBuilder
{
    /// <summary>Constructs Identity, Source, and Contents info sections for a moving platform cell.</summary>
    public static NodeInfo Build(World world, int cell, int cellCount, (int Width, int Height)? cellSize,
        ResourceLibrary library, NodeTableData? nodes)
    {
        var identity = InfoSections.Of("Identity",
            InfoRow.OrNull("World", nodes?.WorldName(world)),
            new InfoRow("Cell", $"{cell + 1} of {cellCount}"));

        var source = InfoSource.Section(library,
            PlatformCompositor.SheetFor(world), PlatformCompositor.PaletteFor(world));

        var contents = InfoSections.Of("Contents",
            InfoRow.OrNull("Cell size", cellSize is { } size ? $"{size.Width}×{size.Height} px" : null),
            new InfoRow("Planes", "Graphic + Collider"));

        return NodeInfos.Of(null, identity, source, contents);
    }
}
