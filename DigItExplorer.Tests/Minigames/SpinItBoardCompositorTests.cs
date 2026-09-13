using System.Security.Cryptography;
using DigItExplorer.Core.Minigames;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies Spin It! minigame board composition against reference game files.</summary>
public class SpinItBoardCompositorTests
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

        var result = SpinItBoardCompositor.Compose(loader, new SpinItRenderOptions(false, false, false));

        Assert.Null(result);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void The_board_is_a_single_still()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        var result = SpinItBoardCompositor.Compose(loader, new SpinItRenderOptions(true, true, true));

        Assert.Equal(320, result!.Width);
        Assert.Equal(200, result.Height);
        Assert.Equal(320 * 200 * 3, result.Rgb.Length);
    }

    /// <summary>Verifies byte-exact pixels for each layer combination against pinned SHA-256 hashes.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory(GameNeeds.FullResourceSet)]
    [InlineData(true, true, true, "D95111B883299BCA252B921AC682A7DAD1DB97C8452D532B601E98116F23D64F")]
    [InlineData(true, false, false, "51B274F86CADC2A3B1346CDF82FE1DA14487C8AC7C8D0E7E33A0373BFCD23F96")]
    [InlineData(true, true, false, "05906167BCB9BBD4667DC6E937EF97DFAD4D74B585F3960F475CA38B1161620C")]
    [InlineData(true, false, true, "9941ED6CB6026471427B7373071EBA260E499E348E0C1DCE229BD6C679FA56A4")]
    public void Composed_pixels_match_the_verified_reference(bool background, bool pointer, bool attempts,
        string expectedSha256)
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        var result = SpinItBoardCompositor.Compose(loader, new SpinItRenderOptions(background, pointer, attempts));

        Assert.Equal(expectedSha256, Convert.ToHexString(SHA256.HashData(result!.Rgb)), ignoreCase: true);
    }
}
