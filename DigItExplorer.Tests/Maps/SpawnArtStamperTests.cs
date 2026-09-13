using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Guards <see cref="EntityArtStamper.StampSpawns"/> against frozen per-level pixels.</summary>
public class SpawnArtStamperTests
{
    /// <summary>Per-level reference SHA-256 hashes of terrain with simulated spawn art stamped.</summary>
    private static readonly (string Level, string Sha)[] Reference =
    [
        ("LVL000", "F2B106E4A8AB99CA7600E0752A64D6EBCE706AB17285D2663E66701B140F9BFE"),
        ("LVL750", "B45B274566FA77DB3907331D2B4D173A7526C60E5FFC79FC20909034FB287AA7"),
    ];

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Spawn_art_matches_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        foreach (var (level, expected) in Reference)
        {
            var fMpf = $"{level}F.MPF";
            var mMpf = $"{level}M.MPF";
            var dlf = library.Read($"{level}.DLF");
            var pal = library.Read($"{level}.PAL");

            var terrain = TerrainCompositor.Compose(library.Read(fMpf), dlf, pal);
            var collision = CollisionCompositor.Compose(library.Read(mMpf), dlf);
            var records = LandingSimulator.SynthesizeSpawns(DlfRecord.ReadAll(dlf), collision, level);

            var rgb = terrain.ToRgb24();
            var coverage = new byte[terrain.Width * terrain.Height];
            EntityArtStamper.StampSpawns(rgb, coverage, terrain.Width, terrain.Height, records, terrain.Palette,
                loadDugSpawn: () => ResolveDugSpawn(library, level),
                loadWoBoss: () => library.TryRead("WO_BOSS.MPF"));

            var actual = Convert.ToHexString(SHA256.HashData(rgb));
            Assert.Equal(expected, actual, ignoreCase: true);
        }
    }

    private static readonly Dictionary<int, char> DugCostume = new() { [0] = '0', [1] = '0', [2] = '6', [3] = '3' };

    private static byte[]? ResolveDugSpawn(ResourceLibrary library, string level)
    {
        int world = level.Length > 3 && char.IsDigit(level[3]) ? (level[3] - '0') / 2 : 0;
        char costume = DugCostume.TryGetValue(world, out var c) ? c : '0';
        return library.TryRead($"DUG{costume}B.SPF");
    }
}
