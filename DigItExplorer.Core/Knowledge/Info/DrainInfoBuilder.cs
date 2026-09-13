using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for drain mechanism states.</summary>
public static class DrainInfoBuilder
{
    /// <summary>Constructs Identity, Source, and Contents info sections for a drain state.</summary>
    public static NodeInfo Build(DrainState state, (int Width, int Height)? cellSize, ResourceLibrary library)
    {
        var identity = InfoSections.Of("Identity",
            new InfoRow("State", state == DrainState.Open ? "Open" : "Sealed"));

        var source = InfoSource.Section(library, DrainCompositor.Sheet, DrainCompositor.Palette);

        var contents = InfoSections.Of("Contents",
            InfoRow.OrNull("Cell size", cellSize is { } size ? $"{size.Width}×{size.Height} px" : null),
            new InfoRow("Planes", "Graphic + Collider"));

        return NodeInfos.Of(NodeNotes.For(NoteKey.Drain(state)), identity, source, contents);
    }
}
