using System.Security.Cryptography;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Guards <see cref="TerrainCompositor"/> against frozen per-level pixels.</summary>
public class TerrainCompositorTests
{
    /// <summary>Per-level reference SHA-256 hashes of reconstructed RGB24 terrain layers.</summary>
    private static readonly (string Level, string Sha)[] Reference =
    [
        ("LVL000", "23e864db5a7f7a911b39f3d8c3adaeb2ecd5b81c24a36817ed0040de9fe32358"), // caves  4x3, height-crop
        ("LVL010", "5b234ecb083ee3efd83e6e7bf7748106c14636c2d8101f2d7f99890290aa50b3"), // caves  4x3, width-crop
        ("LVL090", "a6c4c2494bd4cc5ce8accfdb71757509b5a33eebd60f2d39330ae488e2748edc"), // caves  9x2, wide
        ("LVL200", "1cf0d57a2bf099e7af8060fdf2eac625d5778a2514f8016f99b985ced209da1f"), // water  4x3
        ("LVL400", "ea736e664e59349d6e443425437f243049523511783ae4ac7c6e024e7c1a7083"), // snow   8x2
        ("LVL630", "53f24b5d52830ebe44570785eb0e992afa2d25e6d0e32c108c3c7382f095bcac"), // under  2x4, tall
        ("LVL750", "e88eba2bb741291fff28054668b0d65c5095d65dd1595d8b4175aacc1b464762"), // boss   3x2
        ("LVL900", "f1b3a73955b79c5a6847337512a816d8a85ac50aa6d415742b6b0551988b2419"), // menu   2x1, no crop
    ];

    /// <summary>Guards terrain composition against reference RGB24 pixel hashes across sample levels.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Terrain_reconstruction_matches_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        foreach (var (level, expected) in Reference)
        {
            var fMpf = $"{level}F.MPF";
            Assert.True(library.Contains(fMpf), $"{level}F.MPF missing in {gameDir}");

            var dlf = library.TryRead($"{level}.DLF");
            var pal = library.TryRead($"{level}.PAL");

            var terrain = TerrainCompositor.Compose(library.Read(fMpf), dlf, pal);
            var actual = Convert.ToHexString(SHA256.HashData(terrain.ToRgb24()));

            Assert.Equal(expected, actual, ignoreCase: true);
        }
    }

    /// <summary>Verifies grid block counts and pixel crop dimensions derived from DLF headers.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Grid_shape_is_derived_from_the_dlf_header()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var lvl000 = TerrainCompositor.Compose(
            library.Read("LVL000F.MPF"),
            library.TryRead("LVL000.DLF"),
            library.TryRead("LVL000.PAL"));
        Assert.Equal((4, 3, 12), (lvl000.Cols, lvl000.Rows, lvl000.BlockCount));
        Assert.Equal((1280, 500), (lvl000.Width, lvl000.Height)); // 4*320=1280 (no width crop), height cropped 600→500

        var lvl630 = TerrainCompositor.Compose(
            library.Read("LVL630F.MPF"),
            library.TryRead("LVL630.DLF"),
            library.TryRead("LVL630.PAL"));
        Assert.Equal((2, 4, 8), (lvl630.Cols, lvl630.Rows, lvl630.BlockCount)); // tall, multi-row
        Assert.Equal((480, 660), (lvl630.Width, lvl630.Height));
    }
}
