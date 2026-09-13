using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Slabs;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies SlabCompositor multi-stop screens against reference game files.</summary>
public class SlabCompositorTests
{
    private static ResourceLibrary OpenLibrary()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        return ResourceLibrary.Open(gameDir);
    }

    private static GameFont LoadFont(string gameDir) =>
        GameFont.LoadFromMainExe(Core.Catalog.GameExecutable.Open(gameDir, "MAIN.EXE"));

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void No_layer_requested_composes_nothing()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;
        Assert.True(TestPaths.TryGetGameData(out var data));

        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var font = LoadFont(gameDir);
        var result = SlabCompositor.Compose(loader, font, data.Slab, SlabData.Instructions, 0,
            new SlabRenderOptions(false, false, false, false));

        Assert.Null(result);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_stop_on_both_screens_composes_to_a_full_320x200_canvas()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        Assert.True(TestPaths.TryGetGameData(out var data));
        var font = LoadFont(gameDir);

        var options = new SlabRenderOptions(true, true, true, true);
        foreach (int screen in new[] { SlabData.Instructions, SlabData.Credits })
        {
            int stops = data.Slab.SlabCount(screen);
            Assert.True(stops > 0, $"screen {screen} reports no stop to compose");

            for (int stop = 0; stop < stops; stop++)
            {
                var result = SlabCompositor.Compose(loader, font, data.Slab, screen, stop, options);
                Assert.NotNull(result);
                Assert.Equal(320, result!.Width);
                Assert.Equal(200, result.Height);
                Assert.Equal(320 * 200 * 3, result.Rgb.Length);
            }
        }
    }

    /// <summary>Verifies byte-exact pixels without text for all stops across Instructions and Credits screens.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory(GameNeeds.SupportedBuild)]
    [InlineData(SlabData.Instructions, 0, "05AC90EB8A614E49F09BECA66A11CD403F9627329E64D597A7A5E36F89E69089")]
    [InlineData(SlabData.Instructions, 1, "079ED3A5EDCE797104925B73D727B8003B48905DBBEFA2D2014CAEA11434675C")]
    [InlineData(SlabData.Instructions, 2, "0D1BA1CCCC5714244A2837243BADF71427273F891715E3EDF986CD0CF5B6A1C4")]
    [InlineData(SlabData.Instructions, 3, "1866637C53008C5408C92974672E21F00C568D5A3E47A930442576971C744DFE")]
    [InlineData(SlabData.Instructions, 4, "A622A6898BDE275AFC5F5D8C8A595EC2EE60368E6F0C34E92436EE9D8AF01064")]
    [InlineData(SlabData.Instructions, 5, "32B22509A26E10AEA3989E677E15D7F05B68EA4D4DAF0C53F30CB7871F87E67B")]
    [InlineData(SlabData.Credits, 0, "2183E266CFE629E0DC79E184FEADA74EACF5731EFF6BCB78EA921153C9061B9A")]
    [InlineData(SlabData.Credits, 1, "7BFA5CF2935DDA9894934ECF74BB3BBA2B7143BFBC9D333125D45F90D0A16E15")]
    public void Composed_pixels_without_text_match_the_verified_reference(int screen, int stop, string expectedSha256)
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        Assert.True(TestPaths.TryGetGameData(out var data));
        var font = LoadFont(gameDir);

        var result = SlabCompositor.Compose(loader, font, data.Slab, screen, stop,
            new SlabRenderOptions(true, true, true, false));

        Assert.NotNull(result);
        var actual = Convert.ToHexString(SHA256.HashData(result!.Rgb));
        Assert.Equal(expectedSha256, actual, ignoreCase: true);
    }

    /// <summary>Verifies that the text indicator renders within the measured bounding box (x 126-194, y 180-190).</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Text_indicator_lands_in_the_live_measured_bounding_box()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        Assert.True(TestPaths.TryGetGameData(out var data));
        var font = LoadFont(gameDir);

        var withText = SlabCompositor.Compose(loader, font, data.Slab, SlabData.Instructions, 3,
            new SlabRenderOptions(true, true, true, true));
        var withoutText = SlabCompositor.Compose(loader, font, data.Slab, SlabData.Instructions, 3,
            new SlabRenderOptions(true, true, true, false));

        Assert.NotNull(withText);
        Assert.NotNull(withoutText);

        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue, diffCount = 0;
        for (int y = 0; y < withText!.Height; y++)
        {
            for (int x = 0; x < withText.Width; x++)
            {
                int p = (y * withText.Width + x) * 3;
                if (withText.Rgb[p] == withoutText!.Rgb[p]
                    && withText.Rgb[p + 1] == withoutText.Rgb[p + 1]
                    && withText.Rgb[p + 2] == withoutText.Rgb[p + 2])
                    continue;

                diffCount++;
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
            }
        }

        Assert.Equal(519, diffCount);
        Assert.Equal((126, 194, 180, 190), (minX, maxX, minY, maxY));
    }
}
