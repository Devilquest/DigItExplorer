using System.Security.Cryptography;
using DigItExplorer.Core.Minigames;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies Flip It! minigame board composition against reference game files.</summary>
public class FlipItBoardCompositorTests
{
    private static ResourceLibrary OpenLibrary()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        return ResourceLibrary.Open(gameDir);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void No_layer_requested_composes_nothing()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        var result = FlipItBoardCompositor.Compose(loader, new FlipItRenderOptions(false, false, false, false));

        Assert.Null(result);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void The_board_is_a_single_still()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        var result = FlipItBoardCompositor.Compose(loader, new FlipItRenderOptions(true, true, true, true));

        Assert.Equal(320, result!.Width);
        Assert.Equal(200, result.Height);
        Assert.Equal(320 * 200 * 3, result.Rgb.Length);
    }

    /// <summary>Verifies byte-exact pixels for each layer combination against pinned SHA-256 hashes.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory(GameNeeds.FullResourceSet)]
    [InlineData(true, true, true, true, "D679B81D64C0DA927763C06EF7187C778402A980465756EF55B0EC9114C3A33E")]
    [InlineData(true, false, false, false, "82489260F4EFB444F91EE0317468DF4297691303E6E0936E7E6EACAE624B9E34")]
    [InlineData(true, true, true, false, "BA7CA0A24CAB3B56F15DD4D2DDF74DC6EEAD84BB80DEADE761C48AABE125A3A0")]
    [InlineData(true, false, false, true, "DCA164F8C4D31639D19902BF9155A547DE8391EF5089D99B1D7C5A0AFBCF33D9")]
    public void Composed_pixels_match_the_verified_reference(bool background, bool ropes, bool cards,
        bool attempts, string expectedSha256)
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        var result = FlipItBoardCompositor.Compose(loader, new FlipItRenderOptions(background, ropes, cards, attempts));

        Assert.Equal(expectedSha256, Convert.ToHexString(SHA256.HashData(result!.Rgb)), ignoreCase: true);
    }
}
