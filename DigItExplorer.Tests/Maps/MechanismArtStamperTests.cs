using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Guards <see cref="EntityArtStamper.StampMechanisms"/> against frozen per-level pixels.</summary>
public class MechanismArtStamperTests
{
    /// <summary>Per-level reference SHA-256 hashes of terrain with mechanism graphics stamped.</summary>
    private static readonly (string Level, string Sha)[] Reference =
    [
        ("LVL080", "8780FDF62F162093947789A3237C020812A32D6F4B9C616D16A505939ED6B1DE"),
        ("LVL092", "D6E0A68465AD656AB12EA9C79D42C3AD530F6B31C2EAE60989053A269570729C"),
        ("LVL200", "13FC589B6214E468A39D2595F08106F247FF3EF333F395A64CF1266C7417D1CF"),
        ("LVL209", "1955F107D25DAC1F48360F599993B125465CFFED97C0055A4681587287CB3364"),
        ("LVL420", "FFF4F4E63E7D7B5A7D1BD5CD05A06AD67F5AC9E36FCBBE9EEFFBF04529FD3F3B"),
    ];

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Mechanism_art_matches_reference_pixels()
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
            EntityArtStamper.StampMechanisms(rgb, coverage, terrain.Width, terrain.Height, records, terrain.Palette,
                loadWorldSheet: baseName => ResolveWorldSheet(library, baseName, level),
                loadSheet: name => library.TryRead(name));

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
