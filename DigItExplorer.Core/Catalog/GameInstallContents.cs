using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Core.Catalog;

/// <summary>Represents loaded game resources, fonts, and executable metadata.</summary>
/// <param name="Library">The opened archives, which the caller owns and must dispose.</param>
/// <param name="Font">The game's own glyphs, or null if the executable could not be read.</param>
/// <param name="Data">The game's own names and tables, or null on the same condition.</param>
/// <param name="Skins">Which sheet backs each character in each world, or null when executable data is missing.</param>
/// <param name="MainExe">Which of the three states the game executable turned out to be in.</param>
public sealed record GameInstallContents(
    ResourceLibrary Library,
    GameFont? Font,
    GameData? Data,
    SkinCatalog? Skins,
    MainExeStatus MainExe);
