using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Guards <see cref="EntityArtStamper.StampSpeciesGrids"/> against frozen per-level pixels.</summary>
public class SpeciesGridArtStamperTests
{
    /// <summary>Per-level reference SHA-256 hashes of terrain with aquatics and ghost art stamped.</summary>
    private static readonly (string Level, string Sha)[] Reference =
    [
        ("LVL200", "E344DAE7C0E947E96AA44719917D3E81F63A73D4A845B46A4FC5819805BA19E1"),
        ("LVL040", "024668DEBE456F1D8561825FEF6738B6773BA6CD7D68BDA26BC20F8434461BB0"),
    ];

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Species_grid_art_matches_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        foreach (var (level, expected) in Reference)
        {
            var fMpf = $"{level}F.MPF";
            var dlf = library.Read($"{level}.DLF");
            var pal = library.Read($"{level}.PAL");

            var terrain = TerrainCompositor.Compose(library.Read(fMpf), dlf, pal);
            var records = DlfRecord.ReadAll(dlf);

            var rgb = terrain.ToRgb24();
            var coverage = new byte[terrain.Width * terrain.Height];
            EntityArtStamper.StampSpeciesGrids(rgb, coverage, terrain.Width, terrain.Height, records, terrain.Palette,
                baseName => ResolveWorldSheet(library, baseName, level));

            var actual = Convert.ToHexString(SHA256.HashData(rgb));
            Assert.Equal(expected, actual, ignoreCase: true);
        }
    }

    /// <summary>Resolves a world-suffixed sheet file for the test's own file access.</summary>
    private static byte[]? ResolveWorldSheet(ResourceLibrary library, string baseName, string level)
    {
        int world = level.Length > 3 && char.IsDigit(level[3]) ? (level[3] - '0') / 2 : 0;
        foreach (var name in new[] { $"{baseName}0{world}", baseName, $"{baseName}00" })
        foreach (var ext in new[] { ".SPF", ".MPF" })
        {
            var path = name + ext;
            if (library.Contains(path)) return library.Read(path);
        }
        return null;
    }
}
