using System.Text;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Tests;

/// <summary>Guards Stop It! reel label strings and ink-ramp style operands read from the executable.</summary>
public class StopItLabelsTests
{
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Both_labels_still_land_on_well_formed_strings()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        var labels = data.StopItLabels;
        Assert.NotEmpty(labels.Focused);
        Assert.NotEmpty(labels.Spinning);
        Assert.All(labels.Focused, b => Assert.InRange(b, (byte)' ', (byte)'~'));
        Assert.All(labels.Spinning, b => Assert.InRange(b, (byte)' ', (byte)'~'));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Both_ramp_select_sites_still_hold_a_style_the_font_can_build()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        var labels = data.StopItLabels;
        Assert.InRange(labels.FocusedStyle, 0, GameFont.MaxRampStyle);
        Assert.InRange(labels.UnfocusedStyle, 0, GameFont.MaxRampStyle);
        GameFont.Ramp(labels.FocusedStyle);
        GameFont.Ramp(labels.UnfocusedStyle);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void The_two_reel_states_are_drawn_in_different_colors()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        // What makes the focused reel readable as the focused one is that its ink differs at all; which two
        // ramps the game picked is the fingerprint below, but that they differ is the behavior.
        var labels = data.StopItLabels;
        Assert.NotEqual(labels.FocusedStyle, labels.UnfocusedStyle);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Labels_read_the_text_and_styles_this_build_ships() // build fingerprint, not a guard: see the class summary
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        var labels = data.StopItLabels;
        Assert.Equal("Stop It!", Encoding.Latin1.GetString(labels.Focused));
        Assert.Equal("?", Encoding.Latin1.GetString(labels.Spinning));
        Assert.Equal(0, labels.FocusedStyle);
        Assert.Equal(5, labels.UnfocusedStyle);
    }
}
