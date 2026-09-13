using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for dig-spot marker states.</summary>
public static class DigSpotInfoBuilder
{
    /// <summary>Constructs Identity, Source, and Contents info sections for a dig-spot state.</summary>
    public static NodeInfo Build(DigSpotState state, (int Width, int Height)? cellSize, ResourceLibrary library)
    {
        var identity = InfoSections.Of("Identity",
            new InfoRow("State", StateName(state)));

        var source = InfoSource.Section(library, DigSpotCompositor.Sheet);

        var contents = InfoSections.Of("Contents",
            InfoRow.OrNull("Cell size", cellSize is { } size ? $"{size.Width}×{size.Height} px" : null),
            new InfoRow("Planes", "Graphic"));

        return NodeInfos.Of(NodeNotes.For(NoteKey.DigSpot(state)), identity, source, contents);
    }

    private static string StateName(DigSpotState state) => state switch
    {
        DigSpotState.Exit => "Exit",
        DigSpotState.ExitUnderworld => "Exit (Underworld)",
        DigSpotState.Bonus => "Bonus",
        _ => "Bonus (Sealed)",
    };
}
