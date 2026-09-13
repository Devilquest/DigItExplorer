using DigItExplorer.Core.Ui;

namespace DigItExplorer.Tests;

/// <summary>Verifies viewport framing rules, 1:1 zoom preservation, and reframing state transitions in <see cref="PreviewFramingRule"/>.</summary>
public class PreviewFramingRuleTests
{
    [Fact]
    public void A_new_rule_frames_its_first_render()
    {
        var rule = new PreviewFramingRule();

        Assert.True(rule.NeedsFraming);
        Assert.Equal(new PreviewFraming(Fit: true, Zoom: 2.5), rule.FromStickyZoom(isFitMode: true, zoom: 2.5));
    }

    [Fact]
    public void A_framed_rule_keeps_the_current_zoom_whatever_the_settings_say()
    {
        var rule = new PreviewFramingRule();
        rule.Framed();

        Assert.Equal(PreviewFraming.KeepCurrent, rule.FromStickyZoom(isFitMode: true, zoom: 2.5));
        Assert.Equal(PreviewFraming.KeepCurrent, rule.FromCaptured(new PreviewFraming(Fit: true, Zoom: 1.0)));
    }

    [Fact]
    public void Reading_the_framing_does_not_consume_it()
    {
        var rule = new PreviewFramingRule();

        var first = rule.FromStickyZoom(isFitMode: false, zoom: 3.0);
        var second = rule.FromStickyZoom(isFitMode: false, zoom: 3.0);

        Assert.Equal(first, second);
        Assert.True(rule.NeedsFraming);
    }

    [Fact]
    public void Reframing_after_an_emptied_view_frames_the_next_render_again()
    {
        var rule = new PreviewFramingRule();
        rule.Framed();
        rule.Reframe();

        Assert.Equal(new PreviewFraming(Fit: false, Zoom: 4.5), rule.FromStickyZoom(isFitMode: false, zoom: 4.5));
    }

    [Fact]
    public void A_document_viewed_at_one_to_one_opens_the_next_one_at_one_to_one()
    {
        Assert.Equal(new PreviewFraming(Fit: false, Zoom: 1.0), PreviewFramingRule.CaptureOneToOne(oneToOne: true));
    }

    [Fact]
    public void A_document_viewed_at_any_other_zoom_fits_the_next_one()
    {
        Assert.Equal(new PreviewFraming(Fit: true, Zoom: null), PreviewFramingRule.CaptureOneToOne(oneToOne: false));
    }

    [Fact]
    public void A_captured_framing_survives_a_setting_that_changes_before_the_render()
    {
        var rule = new PreviewFramingRule();
        var captured = PreviewFramingRule.CaptureOneToOne(oneToOne: true);

        // The stray re-fit this guards against: showing the side panel resizes the stage between arming and
        // rendering, and the resulting fit is written back to the setting the capture came from.
        _ = PreviewFramingRule.CaptureOneToOne(oneToOne: false);

        Assert.Equal(new PreviewFraming(Fit: false, Zoom: 1.0), rule.FromCaptured(captured));
    }

    [Fact]
    public void A_re_render_of_a_one_to_one_document_leaves_its_zoom_alone()
    {
        var rule = new PreviewFramingRule();
        var captured = PreviewFramingRule.CaptureOneToOne(oneToOne: true);

        Assert.Equal(new PreviewFraming(Fit: false, Zoom: 1.0), rule.FromCaptured(captured));
        rule.Framed();

        Assert.Equal(PreviewFraming.KeepCurrent, rule.FromCaptured(captured));
    }
}
