using System.Security.Cryptography;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Menu;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies MainMenuCompositor output layers against reference game files.</summary>
public class MainMenuCompositorTests
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
        var doc = MainMenuDocument.Load(loader);
        Assert.NotNull(doc);

        var result = MainMenuCompositor.Compose(doc,
            new MainMenuRenderOptions(false, false, false, new HashSet<(byte, ushort?)>(), false, false, false, false),
            loader, name => loader(name) is not null);

        Assert.Null(result);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Every_layer_composes_to_the_full_640x200_canvas()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;
        var doc = MainMenuDocument.Load(loader);
        Assert.NotNull(doc);

        var keys = doc.Map.Entities.Select(EntityCategories.KeyOf).ToHashSet();
        var result = MainMenuCompositor.Compose(doc,
            new MainMenuRenderOptions(true, true, true, keys, true, true, true, true),
            loader, name => loader(name) is not null);

        Assert.NotNull(result);
        Assert.Equal(640, result!.Width);
        Assert.Equal(200, result.Height);
        Assert.Equal(640 * 200 * 3, result.Rgb.Length);
    }

    /// <summary>Guards main menu composition against reference pixel hashes for each sign page.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory(GameNeeds.FullResourceSet)]
    [InlineData("MENU01.MPF", "cbe174713d36897ba6a5fc5b0221f3235e364d5851aed4d904c103f9d5b06aac")]
    [InlineData("MENU02.MPF", "f67618d939227fa2f6111001be6635d015c05b0e9279538c5e7043ad4286d37d")]
    [InlineData("MENU03.MPF", "7c36ace3a3be08c1fb8308e840fadab9257773cd053a72e52036750b2818304e")]
    [InlineData("MENU04.MPF", "12ad949f44c3dcea2c321246c1b9a98417b1076a9c74c7f03f5be8c740bb5016")]
    public void Composed_pixels_match_the_verified_reference(string signFile, string expectedSha256)
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;
        var doc = MainMenuDocument.Load(loader);
        Assert.NotNull(doc);
        Assert.True(doc.Signs.ContainsKey(signFile), $"{signFile} is missing from this install");

        var keys = doc.Map.Entities.Select(EntityCategories.KeyOf).ToHashSet();
        bool showMain = signFile == "MENU01.MPF", showSetup = signFile == "MENU02.MPF",
            showPlay = signFile == "MENU03.MPF", showIntro = signFile == "MENU04.MPF";
        var result = MainMenuCompositor.Compose(doc,
            new MainMenuRenderOptions(true, true, false, keys, showMain, showSetup, showPlay, showIntro),
            loader, name => loader(name) is not null);

        Assert.NotNull(result);
        var actual = Convert.ToHexString(SHA256.HashData(result!.Rgb));
        Assert.Equal(expectedSha256, actual, ignoreCase: true);
    }
}
