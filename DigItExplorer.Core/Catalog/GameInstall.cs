using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Core.Catalog;

/// <summary>Locates and opens a Dig It! game folder holding game asset archives.</summary>
public static class GameInstall
{
    /// <summary>Opens the game folder at <paramref name="gameDir"/>, loading archive catalog, font, and executable data.</summary>
    /// <param name="gameDir">Directory path containing game archives.</param>
    /// <returns>A <see cref="GameInstallContents"/> instance containing indexed resources and metadata.</returns>
    public static GameInstallContents Open(string gameDir)
    {
        var library = ResourceLibrary.Open(gameDir);
        try
        {
            var (status, font, data) = ReadExecutable(gameDir);

            var skins = data is null ? null : new SkinCatalog(library.TryRead, data.Nodes, data.EntityNames);

            return new GameInstallContents(library, font, data, skins, status);
        }
        catch
        {
            library.Dispose();
            throw;
        }
    }

    private static (MainExeStatus Status, GameFont? Font, GameData? Data) ReadExecutable(string gameDir)
    {
        GameExecutable exe;
        try { exe = GameExecutable.Open(gameDir, ExeLayout.MainExe); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return (MainExeStatus.Unreadable, null, null);
        }

        if (!BuildLayout.TryRecognize(exe, out var layout)) return (MainExeStatus.DifferentBuild, null, null);

        // Font and game data can fail independently if the binary was modified.
        var font = GameFont.TryLoadFromMainExe(exe, layout, out var loadedFont) ? loadedFont : null;
        var data = GameData.TryLoad(exe, layout, out var loadedData) ? loadedData : null;
        return (MainExeStatus.Recognized, font, data);
    }

    /// <summary>The six asset archives, in load order.</summary>
    public static readonly string[] ArchiveNames =
        ["DIGIT0.XRS", "DIGIT1.XRS", "DIGIT2.XRS", "DIGIT3.XRS", "DIGIT4.XRS", "DIGITX.XRS"];

    /// <summary>Validates and resolves a candidate game folder, checking both direct and nested DIGIT directories.</summary>
    /// <param name="folder">The candidate path to inspect.</param>
    /// <param name="gameDir">Outputs the resolved folder containing game archives.</param>
    /// <returns><c>true</c> if valid game archives were found; otherwise, <c>false</c>.</returns>
    public static bool TryUseFolder(string folder, out string gameDir)
    {
        gameDir = "";
        if (string.IsNullOrWhiteSpace(folder) || !Path.IsPathFullyQualified(folder)) return false;

        try
        {
            if (HoldsArchives(folder))
            {
                gameDir = folder;
                return true;
            }

            var nested = Path.Combine(folder, "DIGIT");
            if (HoldsArchives(nested))
            {
                gameDir = nested;
                return true;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
        {
            gameDir = "";
            return false;
        }

        return false;
    }

    // Having at least one archive is sufficient to open the library and browse available assets.
    private static bool HoldsArchives(string dir)
        => Directory.Exists(dir)
            && ArchiveNames.Any(name => File.Exists(Path.Combine(dir, name)));
}
