using System.Security.Cryptography;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Minigames;

namespace DigItExplorer.Tests;

/// <summary>Verifies Find It! minigame board composition against reference game files.</summary>
public class FindItBoardCompositorTests
{
    // Both helpers reach the game through TestPaths: a test calling either needs its own
    // [Trait("Category", "RequiresGame")], or a filtered run reports it as passing while it
    // read nothing.
    private static ResourceLibrary OpenLibrary()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        return ResourceLibrary.Open(gameDir);
    }

    private static (GameFont Font, byte[] StartLabel) ReadGameText()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        Assert.True(GameData.TryLoad(gameDir, out var data));
        var exe = GameExecutable.Open(gameDir, ExeLayout.MainExe);
        return (GameFont.LoadFromMainExe(exe), data.FindItStartLabel);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void No_layer_requested_composes_nothing()
    {
        using var library = OpenLibrary();
        var text = ReadGameText();
        var loader = library.TryRead;

        var result = FindItBoardCompositor.Compose(loader, text.Font, text.StartLabel,
            new FindItRenderOptions(false, false, false, false));

        Assert.Null(result);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void The_board_is_a_single_still()
    {
        using var library = OpenLibrary();
        var text = ReadGameText();
        var loader = library.TryRead;

        var result = FindItBoardCompositor.Compose(loader, text.Font, text.StartLabel,
            new FindItRenderOptions(true, true, true, true));

        Assert.Equal(320, result!.Width);
        Assert.Equal(200, result.Height);
        Assert.Equal(320 * 200 * 3, result.Rgb.Length);
    }

    /// <summary>Verifies byte-exact pixels for each layer combination against pinned SHA-256 hashes.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory(GameNeeds.SupportedBuild)]
    [InlineData(true, true, true, true, "046340A8BC28D11B42A8B2A9006675BA6F5FE9AD507D092283C314DB6BF5BD3E")]
    [InlineData(true, false, false, false, "9E86BE2E9BC311B75DC9356C76DB1206528D81D5AAF948E3AA74B43AEF9446EA")]
    [InlineData(true, true, false, false, "A25BD2892B9DBAA2187F3AF4CFF211181CEBFFDE2C1B4B5C2C092602FE25B026")]
    [InlineData(true, false, true, false, "240CB8D847A7A7F61D60B5245AE73E85C28B535DF5B2494FB1E9CE04AB95880B")]
    public void Composed_pixels_match_the_verified_reference(bool background, bool pieces, bool labels,
        bool attempts, string expectedSha256)
    {
        using var library = OpenLibrary();
        var text = ReadGameText();
        var loader = library.TryRead;

        var result = FindItBoardCompositor.Compose(loader, text.Font, text.StartLabel,
            new FindItRenderOptions(background, pieces, labels, attempts));

        Assert.Equal(expectedSha256, Convert.ToHexString(SHA256.HashData(result!.Rgb)), ignoreCase: true);
    }
}
