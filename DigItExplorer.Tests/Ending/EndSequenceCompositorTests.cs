using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Ending;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.Tests;

/// <summary>Verifies ending sequence screen composition and layer toggles in <see cref="EndSequenceCompositor"/>.</summary>
public class EndSequenceCompositorTests
{
    private static readonly EndSequenceRenderOptions Both = new(ShowBackground: true, ShowText: true);
    private static readonly EndSequenceRenderOptions BackgroundOnly = new(ShowBackground: true, ShowText: false);
    private static readonly EndSequenceRenderOptions TextOnly = new(ShowBackground: false, ShowText: true);
    private static readonly EndSequenceRenderOptions Neither = new(ShowBackground: false, ShowText: false);

    private static GameFont LoadFont(string gameDir) =>
        GameFont.LoadFromMainExe(GameExecutable.Open(gameDir, "MAIN.EXE"));

    /// <summary>Verifies that all ten ending sequence screens compose captions on top of the background art.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Every_screen_composes_and_its_captions_differ_from_the_background_alone()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var path = "ENDSEQ.MPF";
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains(path), $"{path} is missing from this install");

        Assert.True(TestPaths.TryGetGameData(out var data));
        var font = LoadFont(gameDir);
        var endSeq = SheetImage.Read(library.Read(path));
        Assert.Equal(10, endSeq.Frames.Count);

        for (int i = 0; i < endSeq.Frames.Count; i++)
        {
            var withText = EndSequenceCompositor.Compose(endSeq, font, i, data.EndSequence, Both);
            var backgroundOnly = EndSequenceCompositor.Compose(endSeq, font, i, data.EndSequence, BackgroundOnly);
            Assert.NotNull(withText);
            Assert.NotNull(backgroundOnly);
            Assert.Equal(FrameCodec.Width * FrameCodec.Height * 3, withText!.Rgb.Length);

            int diffCount = 0;
            for (int p = 0; p < withText.Rgb.Length; p++)
                if (withText.Rgb[p] != backgroundOnly!.Rgb[p]) diffCount++;
            Assert.True(diffCount > 0, $"screen {i + 1}: captions did not change any pixel over the background alone");
        }
    }

    /// <summary>Verifies that ending sequence composition does not mutate underlying source frames.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Composing_does_not_mutate_the_source_frame()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var path = "ENDSEQ.MPF";
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains(path), $"{path} is missing from this install");

        Assert.True(TestPaths.TryGetGameData(out var data));
        var font = LoadFont(gameDir);
        var endSeq = SheetImage.Read(library.Read(path));
        var rawBefore = (byte[])endSeq.Frames[0].Clone();

        var first = EndSequenceCompositor.Compose(endSeq, font, 0, data.EndSequence, Both);
        var second = EndSequenceCompositor.Compose(endSeq, font, 0, data.EndSequence, Both);

        Assert.Equal(first!.Rgb, second!.Rgb);
        Assert.Equal(rawBefore, endSeq.Frames[0]);
    }

    /// <summary>Verifies that text-only renders cover the caption ink and nothing else.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Text_only_renders_captions_on_an_otherwise_uncovered_canvas()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var path = "ENDSEQ.MPF";
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains(path), $"{path} is missing from this install");

        Assert.True(TestPaths.TryGetGameData(out var data));
        var font = LoadFont(gameDir);
        var endSeq = SheetImage.Read(library.Read(path));

        var result = EndSequenceCompositor.Compose(endSeq, font, 0, data.EndSequence, TextOnly);
        Assert.NotNull(result);
        Assert.False(result!.ShowedBackground);
        Assert.True(result.ShowedText);

        int inkPixels = 0, blankPixels = 0;
        foreach (var coverage in result.Alpha)
        {
            if (coverage == 0) blankPixels++; else inkPixels++;
        }
        Assert.True(inkPixels > 0, "text-only render has no caption ink at all");
        Assert.True(blankPixels > 0, "text-only render covers the whole canvas, leaving nothing transparent");
    }

    /// <summary>Verifies that background-only renders match raw decoded frame pixels without text.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Background_only_matches_the_raw_decoded_frame()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var path = "ENDSEQ.MPF";
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains(path), $"{path} is missing from this install");

        Assert.True(TestPaths.TryGetGameData(out var data));
        var font = LoadFont(gameDir);
        var endSeq = SheetImage.Read(library.Read(path));

        var result = EndSequenceCompositor.Compose(endSeq, font, 0, data.EndSequence, BackgroundOnly);
        Assert.NotNull(result);
        Assert.True(result!.ShowedBackground);
        Assert.False(result.ShowedText);

        var pal = endSeq.Palette;
        var raw = endSeq.Frames[0];
        for (int p = 0; p < raw.Length; p++)
        {
            var (r, g, b) = pal[raw[p]];
            Assert.Equal(r, result.Rgb[p * 3]);
            Assert.Equal(g, result.Rgb[p * 3 + 1]);
            Assert.Equal(b, result.Rgb[p * 3 + 2]);
        }
    }

    /// <summary>Verifies that disabling both background and text layers returns null.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Neither_layer_visible_composes_to_null()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        var path = "ENDSEQ.MPF";
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(library.Contains(path), $"{path} is missing from this install");

        Assert.True(TestPaths.TryGetGameData(out var data));
        var font = LoadFont(gameDir);
        var endSeq = SheetImage.Read(library.Read(path));

        Assert.Null(EndSequenceCompositor.Compose(endSeq, font, 0, data.EndSequence, Neither));
    }
}
