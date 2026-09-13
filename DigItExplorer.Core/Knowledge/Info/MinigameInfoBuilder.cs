using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Minigames;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Constructs Info panel sections for bonus minigame boards.</summary>
public static class MinigameInfoBuilder
{
    /// <summary>Constructs Identity, Source, and Contents info sections for a minigame board.</summary>
    public static NodeInfo Build(MinigameBoard board, ResourceLibrary library)
    {
        var prefix = FilePrefixOf(board);

        var identity = InfoSections.Of("Identity", new InfoRow("Files", $"{prefix}*"));

        var source = InfoSource.Of(library, InfoSource.NamesStartingWith(library, prefix));

        var contents = InfoSections.Of("Contents",
            new InfoRow("Board", $"{MinigameBoardPositions.Width}×{MinigameBoardPositions.Height} px"));

        return NodeInfos.Of(null, identity, source, contents);
    }

    private static string FilePrefixOf(MinigameBoard board) => $"GM{(int)board}_";
}
