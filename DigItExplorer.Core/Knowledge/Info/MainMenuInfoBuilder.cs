using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Menu;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for the main menu screen.</summary>
public static class MainMenuInfoBuilder
{
    private const string Stem = "LVL900";

    /// <summary>Constructs Identity, Source, and Contents info sections for the main menu document.</summary>
    public static NodeInfo Build(MainMenuDocument menu, ResourceLibrary library, EntityNames? entityNames)
    {
        var identity = InfoSections.Of("Identity", new InfoRow("Files", $"{Stem}*"));

        var source = InfoSource.Of(library, InfoSource.NamesStartingWith(library, Stem)
            .Append(menu.BackgroundFile)
            .Concat(menu.Signs.Keys));

        var contentRows = new List<InfoRow?>
        {
            new InfoRow("Terrain", $"{menu.Map.Terrain.Width}×{menu.Map.Terrain.Height} px"),
            new InfoRow("Collision", menu.Map.Collision is not null ? "Present" : "Not present"),
            new InfoRow("Sign pages", menu.Signs.Count.ToString()),
        };
        contentRows.AddRange(EntityCensus.Rows(menu.Map.Entities, entityNames).Select(r => (InfoRow?)r));
        var contents = InfoSections.Of("Contents", [.. contentRows]);

        return NodeInfos.Of(null, identity, source, contents);
    }
}
