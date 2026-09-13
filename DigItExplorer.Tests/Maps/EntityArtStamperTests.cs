using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Guards <see cref="EntityArtStamper.Stamp"/> against frozen per-level pixels.</summary>
public class EntityArtStamperTests
{
    /// <summary>Per-level reference SHA-256 hashes of terrain with static entity art stamped.</summary>
    private static readonly (string Level, string Sha)[] Reference =
    [
        ("LVL000", "3CE6979261FC197A785F3ECA35F53F12E0DB288B0E54F1C6E36ED58ECDF68AF9"),
        ("LVL200", "9D2E407D52014017CDF1211890CE3E1A866FF7CFC7680CF2CA6B0FA01DDFB106"),
        ("LVL630", "0075C0CA2FAE505095832BEE3CA1072E9BB4A50A4987B110AC0B613B16660DAD"),
    ];

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Static_entity_art_matches_reference_pixels()
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
            EntityArtStamper.Stamp(rgb, coverage, terrain.Width, terrain.Height, records, terrain.Palette,
                name => library.TryRead(name));

            var actual = Convert.ToHexString(SHA256.HashData(rgb));
            Assert.Equal(expected, actual, ignoreCase: true);
        }
    }
}
