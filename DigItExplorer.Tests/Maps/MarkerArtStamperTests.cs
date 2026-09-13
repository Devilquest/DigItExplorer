using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Guards <see cref="EntityArtStamper.StampMarkers"/> against frozen per-level pixels.</summary>
public class MarkerArtStamperTests
{
    /// <summary>Per-level reference SHA-256 hashes of terrain with dig-spot and water spawn art stamped.</summary>
    private static readonly (string Level, string Sha)[] Reference =
    [
        ("LVL000", "80AE63A9576904E3FA41A2CC7950AACD2D8FA24CFCDEDD2435A6420645AB07B6"),
        ("LVL600", "CD298BFC7B87CBE37944A8E3244ABF8A7896BC17882B36C7D78273AF074FE9FC"),
        ("LVL221", "2099E2E230A41364A0C976A7A72D58BE7847B54DF8ECB018CBEBEBD97652B6CE"),
    ];

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Marker_art_matches_reference_pixels()
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
            EntityArtStamper.StampMarkers(rgb, coverage, terrain.Width, terrain.Height, records, terrain.Palette,
                level, name => library.TryRead(name));

            var actual = Convert.ToHexString(SHA256.HashData(rgb));
            Assert.Equal(expected, actual, ignoreCase: true);
        }
    }
}
