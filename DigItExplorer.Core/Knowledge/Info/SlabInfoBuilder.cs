using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for Instructions and Credits slab screens.</summary>
public static class SlabInfoBuilder
{
    /// <summary>Constructs Identity, Source, and Contents info sections for a slab stop position.</summary>
    public static NodeInfo Build(int screen, int stopIndex, SlabData slab, ResourceLibrary library)
    {
        var prefix = $"SLB{screen:00}";

        var identity = InfoSections.Of("Identity",
            new InfoRow("Stop", $"{stopIndex + 1} of {slab.SlabCount(screen)}"),
            new InfoRow("Files", $"{prefix}*"));

        var source = InfoSource.Of(library,
            InfoSource.NamesStartingWith(library, prefix).Append(slab.ParallaxFile));

        var contents = InfoSections.Of("Contents",
            new InfoRow("Canvas", $"{slab.Width}×{slab.Height} px"),
            new InfoRow("Stop step", $"{SlabData.SlabStep} px"));

        return NodeInfos.Of(null, identity, source, contents);
    }
}
