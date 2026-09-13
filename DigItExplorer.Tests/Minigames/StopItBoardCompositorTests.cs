using System.Security.Cryptography;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Minigames;

namespace DigItExplorer.Tests;

/// <summary>Verifies Stop It! minigame board composition against reference game files.</summary>
public class StopItBoardCompositorTests
{
    // Both helpers reach the game through TestPaths: a test calling either needs its own
    // [Trait("Category", "RequiresGame")], or a filtered run reports it as passing while it
    // read nothing.
    private static ResourceLibrary OpenLibrary()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        return ResourceLibrary.Open(gameDir);
    }

    private static (GameFont Font, StopItLabels Labels) ReadGameText()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        Assert.True(GameData.TryLoad(gameDir, out var data));
        var exe = GameExecutable.Open(gameDir, ExeLayout.MainExe);
        return (GameFont.LoadFromMainExe(exe), data.StopItLabels);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void No_layer_requested_composes_nothing()
    {
        using var library = OpenLibrary();
        var text = ReadGameText();
        var loader = library.TryRead;

        var result = StopItBoardCompositor.Compose(loader, text.Font, text.Labels,
            new StopItRenderOptions(false, false, false, false));

        Assert.Null(result);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void The_board_is_a_single_still()
    {
        using var library = OpenLibrary();
        var text = ReadGameText();
        var loader = library.TryRead;

        var result = StopItBoardCompositor.Compose(loader, text.Font, text.Labels,
            new StopItRenderOptions(true, true, true, true));

        Assert.Equal(320, result!.Width);
        Assert.Equal(200, result.Height);
        Assert.Equal(320 * 200 * 3, result.Rgb.Length);
    }

    /// <summary>Verifies byte-exact pixels for each layer combination against pinned SHA-256 hashes.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory(GameNeeds.SupportedBuild)]
    [InlineData(true, true, true, true, "FD0039EE01463124ECFA040EC8796A24760D9B2F6DC2319CB9F531E4BE994037")]
    [InlineData(true, false, false, false, "509B0D16AC4883956DB1C953B7F63B69FA016EB493395178CB98AE5DAC1D7466")]
    [InlineData(true, true, false, false, "5EA44B2E4BEB8D74082287267AEC5DFA3E950B67D51AE919B62321C1EFA6B691")]
    [InlineData(true, false, true, false, "D2BBF399A0DE3424E4F88D3EDB7DA52F3CFB81CB905653E425B585FED6E7E010")]
    public void Composed_pixels_match_the_verified_reference(bool background, bool pieces, bool labels,
        bool attempts, string expectedSha256)
    {
        using var library = OpenLibrary();
        var text = ReadGameText();
        var loader = library.TryRead;

        var result = StopItBoardCompositor.Compose(loader, text.Font, text.Labels,
            new StopItRenderOptions(background, pieces, labels, attempts));

        Assert.Equal(expectedSha256, Convert.ToHexString(SHA256.HashData(result!.Rgb)), ignoreCase: true);
    }
}
