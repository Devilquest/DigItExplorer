using System.Security.Cryptography;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Story;

namespace DigItExplorer.Tests;

/// <summary>Verifies story scene layout, scrolling schedule, and text canvas composition against reference frame hashes.</summary>
public class StoryReconstructorTests
{
    // (scene name, world, display-line count, scroll total, text-canvas SHA-256, background SHA-256):
    // all six columns pinned once for every shipped scene.
    private static readonly (string Name, World? World, int Lines, int Total, string CanvasSha, string BgSha)[] Reference =
    [
        ("R00", null, 16, 450,
            "af77fa3f0773645df83c57725cae748e64afee81e4642564ad83d22bd0dbb005",
            "7bc471e5c7e516ceb4f2a7e13cb2039f0633787965b818c63d8c1bdc26bb4532"),
        ("R01", World.Caves, 17, 478,
            "68d8ff75a6351a3a4fdf56c20b441032159b66786d6df8505e2de242daf0bba4",
            "5fedd37faa81b43e842dc9b30e881e1a52362481a4e7ca832b64a41314c9638f"),
        ("R02", World.Water, 16, 450,
            "6e55dfd90974f02f2780fca6b397b8ff74cf67fb22f1b2e89b98e3c502d29918",
            "743746e9c62704a93a32589a7f189a7585f2cb1e981f3ded0a8e69f871a87eac"),
        ("R03", World.Snow, 16, 450,
            "4061e1a380e36557a16b243082369106f7cb2242df3cc8b17a79dd958a5b1407",
            "0a940f0687edebebaa6f41cecf7b9cc6162b386f4973c527d078361e6e43bdff"),
        ("R04", World.Underworld, 21, 534,
            "a83044bde127f35916b7b3cf963de0e6db3391b4ead1f920359ba85ef4e97bf4",
            "c09bf69935ffc91283c748ce13474587f4e895367cb322f59046d6b6cd451128"),
    ];

    /// <summary>Guards scene text layout, total scroll metrics, and background pixel hashes across all five story scenes.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Every_scene_matches_reference_layout_and_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var font = GameFont.LoadFromMainExe(GameExecutable.Open(gameDir, "MAIN.EXE"));

        foreach (var (name, world, lines, total, canvasSha, bgSha) in Reference)
        {
            var background = SheetImage.Read(library.Read($"{name}_SCRN.SPF"));
            var storyTxt = library.Read($"{name}_STRY.TXT");

            var scene = StoryReconstructor.Build(font, background, storyTxt, world);

            Assert.Equal(total, scene.TotalScroll);
            Assert.Equal(canvasSha, Convert.ToHexString(SHA256.HashData(scene.TextCanvas)).ToLowerInvariant());
            Assert.Equal(bgSha, Convert.ToHexString(SHA256.HashData(scene.Background)).ToLowerInvariant());
        }
    }

    /// <summary>Verifies that story scroll schedules begin with an initial 46-tick pause and advance monotonically.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Scroll_schedule_shape_is_consistent()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var font = GameFont.LoadFromMainExe(GameExecutable.Open(gameDir, "MAIN.EXE"));
        var background = SheetImage.Read(library.Read("R01_SCRN.SPF"));
        var storyTxt = library.Read("R01_STRY.TXT");

        var scene = StoryReconstructor.Build(font, background, storyTxt, World.Caves);

        Assert.Equal(0, scene.ScrollOffsets[0]);
        Assert.True(scene.ScrollOffsets[45] == 0, "46 ticks of initial pause before the first scroll step");
        Assert.Equal(1, scene.ScrollOffsets[46]);
        Assert.True(scene.ScrollOffsets[^1] > scene.TotalScroll);
        for (int i = 1; i < scene.ScrollOffsets.Length; i++)
            Assert.True(scene.ScrollOffsets[i] - scene.ScrollOffsets[i - 1] is 0 or 1);
    }

    /// <summary>Verifies reconstruction of the standalone INTRODRG intro scene using explicit mode 1 layout.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Mode_overload_matches_reference_for_introdrg()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var font = GameFont.LoadFromMainExe(GameExecutable.Open(gameDir, "MAIN.EXE"));
        var background = SheetImage.Read(library.Read("INTRODRG.SPF"));
        var storyTxt = library.Read("INTRODRG.TXT");

        var scene = StoryReconstructor.Build(font, background, storyTxt, mode: 1);

        Assert.Equal(394, scene.TotalScroll);
        Assert.Equal("04a6e4434fcdc476b3b3856c18794f7f7ec0583655fa84e97e55d8433e8a408f",
            Convert.ToHexString(SHA256.HashData(scene.TextCanvas)).ToLowerInvariant());
        Assert.Equal("d7b0d299bb0f524f637e4b290c2522771e1778acf584299b3e184f3d0318d4b6",
            Convert.ToHexString(SHA256.HashData(scene.Background)).ToLowerInvariant());

        (int Tick, string Sha)[] samples =
        [
            (0, "d7b0d299bb0f524f637e4b290c2522771e1778acf584299b3e184f3d0318d4b6"),
            (46, "6147413a2d3ad0b718b81de65e3f4bc7cb777ebe0e96cbaebf3b46f5c368f7a1"),
            (100, "03f08ecb19814ef895a034adc6971813d8c2d84b8a1da7d8fab9517867c0718d"),
        ];
        foreach (var (tick, sha) in samples)
            Assert.Equal(sha, Convert.ToHexString(SHA256.HashData(StoryReconstructor.Compose(scene, tick))).ToLowerInvariant());
    }

    /// <summary>Guards story scene frame composition against reference pixel hashes at sampled timeline ticks.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Compose_matches_reference_frames_at_sample_ticks()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var font = GameFont.LoadFromMainExe(GameExecutable.Open(gameDir, "MAIN.EXE"));
        var background = SheetImage.Read(library.Read("R01_SCRN.SPF"));
        var storyTxt = library.Read("R01_STRY.TXT");
        var scene = StoryReconstructor.Build(font, background, storyTxt, World.Caves);

        (int Tick, string Sha)[] samples =
        [
            (0, "5fedd37faa81b43e842dc9b30e881e1a52362481a4e7ca832b64a41314c9638f"),    // no text visible yet
            (46, "5fedd37faa81b43e842dc9b30e881e1a52362481a4e7ca832b64a41314c9638f"),   // still hidden mid-hold
            (100, "a429d66442deee76358d8dceb92a1a5d234913a04fc171b052fe34230d85933f"),
            (653, "fe45d9247c614a365ba830b1232e3ec25d401d27152e4ad0356b8eeeccbc17e9"),
            (1306, "5fedd37faa81b43e842dc9b30e881e1a52362481a4e7ca832b64a41314c9638f"), // scrolled past the last line
        ];

        foreach (var (tick, sha) in samples)
        {
            var frame = StoryReconstructor.Compose(scene, tick);
            Assert.Equal(sha, Convert.ToHexString(SHA256.HashData(frame)).ToLowerInvariant());
        }
    }
}
