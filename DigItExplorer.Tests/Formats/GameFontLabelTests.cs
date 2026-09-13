using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.Tests;

/// <summary>Tests nearest-neighbor integer scaling in <see cref="GameFontLabel.DrawScaled"/>.</summary>
public class GameFontLabelTests
{
    private static GameFont Load()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        return GameFont.LoadFromMainExe(GameExecutable.Open(gameDir, "MAIN.EXE"));
    }

    private const int Style = 2;

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Scale_1_draws_exactly_what_the_unscaled_blit_draws()
    {
        var font = Load();

        byte[] text = ">402"u8.ToArray();
        var ramp = GameFont.Ramp(Style);
        const int w = 120, h = 40;

        var unscaled = new byte[w * h];
        font.Draw(unscaled, w, h, 7, 5, text, ramp);

        var scaled = new byte[w * h];
        GameFontLabel.DrawScaled(font, scaled, w, h, 7, 5, text, ramp, 1);

        Assert.Equal(unscaled, scaled);
    }

    /// <summary>Verifies that scaling expands each glyph pixel into an N×N pixel block preserving exact ramp colors.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    [InlineData(2)]
    [InlineData(3)]
    public void Scaling_expands_each_ink_pixel_into_a_solid_block(int scale)
    {
        var font = Load();

        byte[] text = ">402"u8.ToArray();
        var ramp = GameFont.Ramp(Style);
        const int w = 240, h = 80;

        var unscaled = new byte[w * h];
        font.Draw(unscaled, w, h, 0, 0, text, ramp);

        var scaled = new byte[w * h];
        GameFontLabel.DrawScaled(font, scaled, w, h, 0, 0, text, ramp, scale);

        Assert.Equal(unscaled.Count(v => v != 0) * scale * scale, scaled.Count(v => v != 0));
        Assert.Equal(unscaled.Where(v => v != 0).ToHashSet(), scaled.Where(v => v != 0).ToHashSet());
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Scaled_text_landing_outside_the_mask_is_dropped_rather_than_throwing()
    {
        var font = Load();

        var mask = new byte[8 * 8];
        var ramp = GameFont.Ramp(Style);

        GameFontLabel.DrawScaled(font, mask, 8, 8, -400, -400, "9"u8, ramp, 3);
        GameFontLabel.DrawScaled(font, mask, 8, 8, 400, 400, "9"u8, ramp, 3);
        GameFontLabel.DrawScaled(font, mask, 8, 8, 0, 0, ReadOnlySpan<byte>.Empty, ramp, 3);

        Assert.All(mask, v => Assert.Equal(0, v));
    }
}
