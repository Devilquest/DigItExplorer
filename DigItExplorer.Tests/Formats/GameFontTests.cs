using System.Security.Cryptography;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="GameFont"/> against the game's own font decode and metric measurement.</summary>
public class GameFontTests
{
    private static GameFont Load()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        return GameFont.LoadFromMainExe(GameExecutable.Open(gameDir, "MAIN.EXE"));
    }

    /// <summary>Guards decoded font glyph bitmaps and per-character width/height metrics against reference hashes.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Decoded_page_and_measured_metrics_match_reference()
    {
        var font = Load();

        Assert.Equal("95781536794146fe39e97524fcaeadda3eb75bc853dc99bbb8efb82fce0fe619",
            Convert.ToHexString(SHA256.HashData(font.Page)).ToLowerInvariant());

        var widths = new byte[128];
        var heights = new byte[128];
        for (int c = 0; c < 128; c++)
        {
            widths[c] = (byte)font.Widths[c];
            heights[c] = (byte)font.Heights[c];
        }
        Assert.Equal("668e7e5c4384f5aacc43fd503e45030e9aab5ac5a67ccf0913dcb597cf8680c3",
            Convert.ToHexString(SHA256.HashData(widths)).ToLowerInvariant());
        Assert.Equal("f0536b4308207762f19dc49c264bc479f74e9bc6b5baf2633e63c881d6156f5d",
            Convert.ToHexString(SHA256.HashData(heights)).ToLowerInvariant());
    }

    /// <summary>Verifies proportional glyph widths for sample characters against game values.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Sample_widths_match_documented_values()
    {
        var font = Load();

        Assert.Equal(2, font.Widths[(byte)'I']);
        Assert.Equal(2, font.Widths[(byte)'.']);
        Assert.Equal(6, font.Widths[(byte)'A']);
        Assert.Equal(10, font.Widths[(byte)'M']);
        Assert.Equal(10, font.Widths[(byte)'W']);
        Assert.Equal(4, font.Widths[(byte)' ']); // BlankWidth fallback
    }

    /// <summary>Verifies that font color ramps 1..7 match palette base indices from executable table DS:0x0BD6.</summary>
    [Fact]
    public void Ramp_reproduces_seg3_0x0370()
    {
        Assert.Same(GameFont.TextRamp, GameFont.Ramp(0));

        byte[][] expected =
        [
            [0, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 0, 0, 0], // style 1
            [0, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 0, 0, 0], // style 2
            [0, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 0, 0, 0], // style 3
            [0, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 0, 0, 0], // style 4
            [0, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 0, 0, 0], // style 5
            [0, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 0, 0, 0], // style 6
            [0, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 0, 0, 0], // style 7
        ];
        for (int style = 1; style <= 7; style++)
            Assert.Equal(expected[style - 1], GameFont.Ramp(style));
    }

    /// <summary>Verifies that the bright ramp shifts every ink level onto the tone above it, so the amber
    /// style paints its first palette index where the installed ramp paints its second.</summary>
    [Fact]
    public void BrightRamp_shifts_every_ink_level_onto_the_tone_above_it()
    {
        Assert.Equal([0, 156, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 0, 0, 0],
            GameFont.BrightRamp(5));
        for (int style = 0; style <= GameFont.MaxRampStyle; style++)
        {
            var installed = GameFont.Ramp(style);
            var bright = GameFont.BrightRamp(style);
            for (int level = 2; level <= 12; level++)
                Assert.Equal(installed[level - 1], bright[level]);
        }
    }

    /// <summary>Verifies that building a bright ramp for style 0 leaves the shared default ramp untouched.</summary>
    [Fact]
    public void BrightRamp_does_not_write_through_to_the_shared_default_ramp()
    {
        var before = (byte[])GameFont.TextRamp.Clone();
        GameFont.BrightRamp(0);
        Assert.Equal(before, GameFont.TextRamp);
    }

    /// <summary>Verifies that centered and right-aligned drawing match precomputed pen positions in <see cref="GameFont.Draw"/>.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void DrawCentered_and_DrawRightAligned_match_a_manually_positioned_Draw()
    {
        var font = Load();

        byte[] text = "Supreme Spurkasaur"u8.ToArray();
        const int w = 64, h = 16;

        var viaCentered = new byte[w * h];
        font.DrawCentered(viaCentered, w, h, 40, 2, text);
        var viaManualCenter = new byte[w * h];
        font.Draw(viaManualCenter, w, h, 40 - (font.TextWidth(text) >> 1), 2, text);
        Assert.Equal(viaManualCenter, viaCentered);

        var viaRight = new byte[w * h];
        font.DrawRightAligned(viaRight, w, h, 60, 2, text);
        var viaManualRight = new byte[w * h];
        font.Draw(viaManualRight, w, h, 60 - font.TextWidth(text), 2, text);
        Assert.Equal(viaManualRight, viaRight);
    }
}
