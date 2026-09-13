using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Guards <see cref="EntityArtStamper.StampEnemies"/> against frozen per-level pixels.</summary>
public class EnemyArtStamperTests
{
    /// <summary>Per-level reference SHA-256 hashes of terrain with grid-based enemy art stamped.</summary>
    private static readonly (string Level, string Sha)[] Reference =
    [
        ("LVL000", "BA5D5E5B9500E64B7980E38F7F7A158584901FF6421C5AF6E7661C470F39C298"),
        ("LVL400", "14E03DCB39172C5CC34AEC7C9ABDBBEE7D8A3764C54618A0017DE065AED46EF1"),
        ("LVL600", "858B32FD030D6898E5070399875BEFB12A8677DA9D0E4CC8A53BE04DBA66DF33"),
    ];

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Enemy_grid_art_matches_reference_pixels()
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
            EntityArtStamper.StampEnemies(rgb, coverage, terrain.Width, terrain.Height, records, terrain.Palette,
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
