using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Guards <see cref="WorldMapSignStamper"/> against frozen per-map pixels.</summary>
public class WorldMapSignStamperTests
{
    /// <summary>Per-map reference SHA-256 hashes of base layers with stamped sign graphics.</summary>
    private static readonly (int Map, string Sha)[] Reference =
    [
        (0, "6b90b05026a3e797de685b94a512eb0a9fbe835636c327626e34c43b82af3cfb"),
        (1, "9f1d0344de1cbb727173b240b38c6aa696b2635d6e2f02f4e41eaca2bcf6930a"),
        (2, "e2c259029fb8153f1e01fe1cb1a9500e42b6de4fc279b837f822515d4c70f9f0"),
        (3, "e4d9a823699f53849dca5be04bf0d51ca222eef2e35c6af041f7d5a3c402f4a0"),
    ];

    /// <summary>Guards world map sign stamping against reference RGB24 pixel hashes across maps.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Signs_stamped_on_base_layer_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(TestPaths.TryGetGameData(out var data));

        foreach (var (map, expected) in Reference)
        {
            var world = (World)map;
            var ldMpf = $"MAP{map:D2}LD.MPF";
            var pMpf = $"MAP{map:D2}P.MPF";
            var sgnPath = $"MAP{map:D2}SGN.SPF";
            Assert.True(library.Contains(ldMpf), $"MAP{map:D2}LD.MPF missing in {gameDir}");
            Assert.True(library.Contains(sgnPath), $"MAP{map:D2}SGN.SPF missing in {gameDir}");

            var pal = library.Read($"MAP{map:D2}.PAL");
            var worldMap = WorldMapCompositor.ComposeBase(library.Read(ldMpf), pal);
            var rgb = worldMap.ToRgb24();
            var coverage = new byte[worldMap.Width * worldMap.Height];

            var path = WorldMapCompositor.ComposePath(library.Read(pMpf));
            var sgnFrame = SheetImage.Read(library.Read(sgnPath)).Frames[0];

            var allSignTypes = new HashSet<SignType> { SignType.Level, SignType.Checkpoint, SignType.Gate, SignType.Trace };
            WorldMapSignStamper.Stamp(rgb, coverage, worldMap.Width, worldMap.Height, path, world,
                index => data.Nodes.Node(world, index)?.Sign ?? SignType.Level,
                sgnFrame, worldMap.Palette, allSignTypes);

            var actual = Convert.ToHexString(SHA256.HashData(rgb));
            Assert.Equal(expected, actual, ignoreCase: true);
        }
    }
}
