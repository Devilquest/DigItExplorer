using System.Security.Cryptography;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Guards <see cref="WorldMapCompositor"/> against frozen per-map pixels.</summary>
public class WorldMapCompositorTests
{
    /// <summary>Per-map reference SHA-256 hashes of reconstructed base layers.</summary>
    private static readonly (int Map, string Sha)[] BaseReference =
    [
        (0, "1b1633c85e51d5531c2dc8e04a0fa6bf10c94ede214b8c7e982e6e403c533583"), // caves,      3 blocks
        (1, "054336725268ddf93314305fcb25599be6deeb0ea15bfe3e0198db023c4e5af7"), // water,      3 blocks
        (2, "80a79e86a9df4e155ff9e34c212c36311e52f2ddc6757fc657ddfd15024d1201"), // snow,       3 blocks
        (3, "7c8284ea41334ea563582363f001d2805b855350d07841e77311a06c27440a42"), // underworld, 3 blocks
        (4, "d6ab0b48d87573d5f5d92d5c9bf09f5a28455cf003440c09c61173ed5c63d440"), // boss,       1 block (trimmed)
    ];

    /// <summary>Guards world map base layer composition against reference RGB24 pixel hashes across maps.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Base_layer_reconstruction_matches_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        foreach (var (map, expected) in BaseReference)
        {
            var ldMpf = $"MAP{map:D2}LD.MPF";
            Assert.True(library.Contains(ldMpf), $"MAP{map:D2}LD.MPF missing in {gameDir}");

            var pal = library.TryRead($"MAP{map:D2}.PAL");

            var worldMap = WorldMapCompositor.ComposeBase(library.Read(ldMpf), pal);
            var actual = Convert.ToHexString(SHA256.HashData(worldMap.ToRgb24()));

            Assert.Equal(expected, actual, ignoreCase: true);
        }
    }

    /// <summary>Verifies that trailing empty blocks are trimmed strictly for the boss world map.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Trailing_empty_blocks_are_trimmed_only_for_the_boss_screen()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var caves = WorldMapCompositor.ComposeBase(library.Read("MAP00LD.MPF"));
        Assert.Equal((3, 960, 200), (caves.BlockCount, caves.Width, caves.Height));

        var boss = WorldMapCompositor.ComposeBase(library.Read("MAP04LD.MPF"));
        Assert.Equal((1, 320, 200), (boss.BlockCount, boss.Width, boss.Height));
    }

    /// <summary>Per-world reference SHA-256 hashes of decoded front planes.</summary>
    private static readonly (int Map, string Sha)[] FrontReference =
    [
        (0, "697df87821c0678c468af7e36d67d6028eaaea8df41b3100b7a6d6eef69fe183"),
        (1, "c3598b956abe391e9a526a75479e01a026506af7249ef694550bc448adcc52e5"),
        (2, "32349b085afb96e0965700dd51580102ba0066d4049f4499c4fc6412d856fb72"),
        (3, "31a9e1d44875a273b0c61efca651ee8986d74326de4a0d84b2e55525c9bb2523"),
    ];

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Front_layer_decode_matches_reference_and_derives_alpha_from_index_zero()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        foreach (var (map, expected) in FrontReference)
        {
            var fMpf = library.Read($"MAP{map:D2}F.MPF");
            var front = WorldMapCompositor.ComposeFront(fMpf);

            Assert.Equal((960, 200), (front.Width, front.Height));
            Assert.Equal(expected, Convert.ToHexString(SHA256.HashData(front.Indices)), ignoreCase: true);

            Assert.NotNull(front.Alpha);
            for (int i = 0; i < front.Indices.Length; i++)
                Assert.Equal(front.Indices[i] == 0 ? 0 : 255, front.Alpha![i]);
        }
    }

    /// <summary>Per-texture reference SHA-256 hashes of untiled source frames.</summary>
    private static readonly (int Map, string File, string Sha)[] RawFrameReference =
    [
        (0, "SKY", "24bd8c875a83aeb509b0fc6b9ee489c07eb45703a3801a3dd84658f5683a2bab"),
        (2, "SKY", "24bd8c875a83aeb509b0fc6b9ee489c07eb45703a3801a3dd84658f5683a2bab"),
        (0, "BKF", "112d933bd9eb8761a970ac3a606fac57ce82df18533171b6a47cdb6b6ae30ba9"),
        (0, "BKM", "3be9fe777af9087c74a9384dbedd9c2c4d4ba246c773dc4bbb3d4a199f4cbabf"),
        (2, "BKF", "9637634081776c78adfc241273de9266bb7f972cc1f671830ac7cfbd37553032"),
        (2, "BKM", "570cd7b8c198963ed1643d21ae0ce6046598362b010351893ef7aa92607f69cf"),
        (1, "BK", "b5c97607c6721a3f6bd3b558b1137ac84051a59fe3bbf1e4da4e1eb55b2281fa"),
        (3, "BK", "7f590a41c0924e64003d6c8a2b22413b2629da33945067b0fdeded921a3309dd"),
    ];

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Sky_and_bk_tiling_repeats_the_reference_frame_exactly_across_all_3_blocks()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        foreach (var (map, file, expected) in RawFrameReference)
        {
            var bytes = library.Read($"MAP{map:D2}{file}.SPF");
            var tiled = file == "SKY"
                ? WorldMapCompositor.ComposeSky(bytes, 960)
                : WorldMapCompositor.ComposeBackground(bytes, mask: null, 960); // decode only: BKM handling is a separate case below

            // Extract block 0 (the untiled source frame) back out and hash it: proves the decode itself is
            // byte-exact against the reference, independent of the tiling assumption.
            var block0 = ExtractBlock(tiled.Indices, canvasWidth: 960, blockIndex: 0);
            Assert.Equal(expected, Convert.ToHexString(SHA256.HashData(block0)), ignoreCase: true);

            // Self-consistency: every block must be an exact repeat of block 0.
            Assert.Equal(block0, ExtractBlock(tiled.Indices, 960, 1));
            Assert.Equal(block0, ExtractBlock(tiled.Indices, 960, 2));
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Masked_bk_derives_alpha_from_bkm_and_unmasked_bk_is_fully_opaque()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var bkf = library.Read("MAP00BKF.SPF");
        var bkm = library.Read("MAP00BKM.SPF");
        var masked = WorldMapCompositor.ComposeBackground(bkf, bkm, 960);
        Assert.NotNull(masked.Alpha);
        var maskedBlock0Mask = ExtractBlock(masked.Alpha!, 960, 0);
        var rawMaskFrame = DigItExplorer.Core.Formats.SheetImage.Read(bkm).Frames[0];
        for (int i = 0; i < rawMaskFrame.Length; i++)
            Assert.Equal(rawMaskFrame[i] == 0 ? 255 : 0, maskedBlock0Mask[i]);

        var unmaskedBytes = library.Read("MAP01BK.SPF");
        var unmasked = WorldMapCompositor.ComposeBackground(unmaskedBytes, mask: null, 960);
        Assert.Null(unmasked.Alpha);
    }

    private static byte[] ExtractBlock(byte[] canvas, int canvasWidth, int blockIndex)
    {
        const int blockWidth = 320, height = 200;
        var block = new byte[blockWidth * height];
        for (int y = 0; y < height; y++)
            Array.Copy(canvas, y * canvasWidth + blockIndex * blockWidth, block, y * blockWidth, blockWidth);
        return block;
    }
}
