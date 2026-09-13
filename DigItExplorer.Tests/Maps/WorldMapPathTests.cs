using System.Security.Cryptography;
using System.Text;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Guards <see cref="WorldMapCompositor.ComposePath"/> against frozen per-map point sets.</summary>
public class WorldMapPathTests
{
    /// <summary>Per-map reference point counts, point set SHA-256 hashes, and node coordinates.</summary>
    private static readonly (int Map, int PathCount, string PathSha, int StopCount, string StopSha, (int Index, int X, int Y)[] Nodes)[] Reference =
    [
        (0, 1213, "5fc9c332610be32871b46c5f9d3e233f03e0887764a8850764b5d50c6247be21",
                40, "4c9f8ec73db34192c64c033985e8127c11eadebba06f8abb2096d9f3159e1d17",
            [(0, 523, 73), (1, 638, 61), (2, 822, 86), (3, 899, 100), (4, 813, 154), (5, 756, 154),
             (6, 571, 154), (7, 466, 156), (8, 330, 112), (9, 248, 119), (10, 83, 127)]),
        (1, 887, "44e380a0ee01b310963f864034732b4e4367cf200c74855c8672a9374f72f59e",
                36, "0b5855578bd83c8d54589b8ac8cec223b1c3ff68d9adad207edf888030748dd4",
            [(0, 45, 59), (1, 136, 128), (2, 228, 123), (3, 339, 153), (4, 401, 72), (5, 495, 135),
             (6, 601, 128), (7, 736, 80), (8, 825, 149), (9, 917, 69)]),
        (2, 1398, "ba50ec076d233b7fa9d56fb77abaf64bd6c0328ec5f3ada05d891ffb0cfbcdac",
                48, "63469903b98e9aa650d16703baa104ccebdd14c3c7cbe3b1960a37c78d09b6a4",
            [(0, 530, 75), (1, 444, 78), (2, 267, 84), (3, 64, 111), (4, 214, 161), (5, 314, 164),
             (6, 412, 149), (7, 496, 167), (8, 583, 146), (9, 698, 148), (10, 765, 161), (11, 860, 165), (12, 896, 105)]),
        (3, 898, "306629b2176ba6bd342ee9e1eaef69e353f8e40717c7a6cd7409659e773a4b40",
                32, "025be153b63246ae390569e8eab318c483d2785c24f618ba00a860c52af9ce47",
            [(0, 928, 130), (1, 839, 116), (2, 719, 165), (3, 616, 172), (4, 488, 132), (5, 384, 148),
             (6, 250, 169), (7, 125, 85), (8, 38, 106)]),
        (4, 439, "6bdd6cc6f643e13c65be5509462ad35eff72018a9ec64e64073fba35b6900896",
                4, "62847a1b040c891f404e061d7e2919f2df3376f4a35be8887f546dca480726ab",
            [(0, 153, 115)]),
    ];

    /// <summary>Guards path plane point counts, point set hashes, and node coordinates across all world maps.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Path_plane_decoding_matches_reference()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        foreach (var (map, pathCount, pathSha, stopCount, stopSha, nodes) in Reference)
        {
            var pMpf = $"MAP{map:D2}P.MPF";
            Assert.True(library.Contains(pMpf), $"MAP{map:D2}P.MPF missing in {gameDir}");

            var result = WorldMapCompositor.ComposePath(library.Read(pMpf));

            Assert.Equal(pathCount, result.PathPixels.Count);
            Assert.Equal(pathSha, HashSortedTuples(result.PathPixels), ignoreCase: true);

            Assert.Equal(stopCount, result.StopPixels.Count);
            Assert.Equal(stopSha, HashSortedTuples(result.StopPixels), ignoreCase: true);

            Assert.Equal(nodes.Length, result.Nodes.Count);
            foreach (var (index, x, y) in nodes)
            {
                Assert.True(result.Nodes.TryGetValue(index, out var pos), $"MAP{map:D2} node {index} missing");
                Assert.Equal((x, y), pos);
            }
        }
    }

    /// <summary>Computes a deterministic SHA-256 hash over an ordered list of point coordinates.</summary>
    private static string HashSortedTuples(IReadOnlyList<(int X, int Y)> points)
    {
        var sorted = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        var sb = new StringBuilder("[");
        for (int i = 0; i < sorted.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append('(').Append(sorted[i].X).Append(", ").Append(sorted[i].Y).Append(')');
        }
        sb.Append(']');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
    }
}
