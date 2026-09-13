using DigItExplorer.Core.Ui;

namespace DigItExplorer.Tests;

/// <summary>Tests window placement and bounds clamping rules in <see cref="WindowPlacementRule.Resolve"/>.</summary>
public sealed class WindowPlacementRuleTests
{
    // A single generous monitor, used by every test that is not itself about monitor geometry.
    private static readonly WindowRect RoomyWorkArea = new(Left: 0, Top: 0, Width: 1920, Height: 1080);
    private const double MinWidth = 900;
    private const double MinHeight = 600;

    [Fact]
    public void Check0_a_missing_field_falls_back()
        => Assert.Null(WindowPlacementRule.Resolve(null, MinWidth, MinHeight, RoomyWorkArea, RoomyWorkArea));

    [Theory]
    [InlineData(double.NaN, 100, 800, 600)] // Left NaN
    [InlineData(100, double.PositiveInfinity, 800, 600)] // Top infinite
    [InlineData(100, 100, -800, 600)] // negative width
    [InlineData(100, 100, 800, 0)] // zero height
    public void Check1_NaN_infinity_or_a_non_positive_size_falls_back(double left, double top, double width, double height)
    {
        var saved = new WindowRect(left, top, width, height);
        Assert.Null(WindowPlacementRule.Resolve(saved, MinWidth, MinHeight, RoomyWorkArea, RoomyWorkArea));
    }

    [Fact]
    public void Check1_a_negative_Left_or_Top_is_legitimate_and_survives()
    {
        // A monitor to the left of the primary sits at negative desktop coordinates.
        var leftMonitor = new WindowRect(Left: -1920, Top: 0, Width: 1920, Height: 1080);
        var virtualScreen = new WindowRect(Left: -1920, Top: 0, Width: 1920 + 1920, Height: 1080);
        var saved = new WindowRect(Left: -1800, Top: 100, Width: 1000, Height: 700);

        var resolved = WindowPlacementRule.Resolve(saved, MinWidth, MinHeight, leftMonitor, virtualScreen);

        Assert.Equal(saved, resolved);
    }

    [Fact]
    public void Check2_a_rectangle_smaller_than_the_minimum_is_clamped_up()
    {
        var saved = new WindowRect(Left: 100, Top: 100, Width: 200, Height: 150);

        var resolved = WindowPlacementRule.Resolve(saved, MinWidth, MinHeight, RoomyWorkArea, RoomyWorkArea);

        Assert.Equal(new WindowRect(100, 100, MinWidth, MinHeight), resolved);
    }

    [Fact]
    public void Check3_landing_on_no_monitor_at_all_falls_back()
    {
        var saved = new WindowRect(Left: 100, Top: 100, Width: 1000, Height: 700);

        var resolved = WindowPlacementRule.Resolve(saved, MinWidth, MinHeight, monitorWorkArea: null, RoomyWorkArea);

        Assert.Null(resolved);
    }

    [Fact]
    public void Check4_less_than_the_grabbable_overlap_falls_back()
    {
        // Only 50 DIP of the window's caption strip would sit on the work area, below the 100 DIP floor.
        var saved = new WindowRect(Left: RoomyWorkArea.Left - 950, Top: 100, Width: 1000, Height: 700);

        var resolved = WindowPlacementRule.Resolve(saved, MinWidth, MinHeight, RoomyWorkArea, RoomyWorkArea);

        Assert.Null(resolved);
    }

    [Fact]
    public void Check4_a_top_edge_above_the_work_areas_top_falls_back()
    {
        var saved = new WindowRect(Left: 100, Top: RoomyWorkArea.Top - 50, Width: 1000, Height: 700);

        var resolved = WindowPlacementRule.Resolve(saved, MinWidth, MinHeight, RoomyWorkArea, RoomyWorkArea);

        Assert.Null(resolved);
    }

    [Fact]
    public void Check5_wider_than_the_target_work_area_is_clamped_to_fit()
    {
        // Not entirely inside the (single-monitor) virtual screen either, which is what triggers the clamp.
        var saved = new WindowRect(Left: 1000, Top: 100, Width: 2000, Height: 700);

        var resolved = WindowPlacementRule.Resolve(saved, MinWidth, MinHeight, RoomyWorkArea, RoomyWorkArea);

        Assert.Equal(RoomyWorkArea.Width, resolved!.Value.Width);
        // Moved fully inside: neither edge escapes the work area.
        Assert.True(resolved.Value.Left >= RoomyWorkArea.Left);
        Assert.True(resolved.Value.Right <= RoomyWorkArea.Right);
    }

    [Fact]
    public void Check5_a_rectangle_spanning_two_monitors_inside_the_virtual_screen_comes_back_untouched()
    {
        var primary = new WindowRect(Left: 0, Top: 0, Width: 1920, Height: 1080);
        var virtualScreen = new WindowRect(Left: 0, Top: 0, Width: 1920 * 2, Height: 1080);
        // Wider than the primary's own work area, but the combined desktop still holds it entirely.
        var saved = new WindowRect(Left: 100, Top: 100, Width: 2500, Height: 700);

        var resolved = WindowPlacementRule.Resolve(saved, MinWidth, MinHeight, primary, virtualScreen);

        Assert.Equal(saved, resolved);
    }

    [Fact]
    public void The_minimum_outranks_a_work_area_smaller_than_it()
    {
        // A 1366x768 laptop at 150% scaling: roughly 911x512 DIP of work area, shorter than MinHeight.
        var smallWorkArea = new WindowRect(Left: 0, Top: 0, Width: 911, Height: 512);
        var saved = new WindowRect(Left: 0, Top: 0, Width: 200, Height: 150);

        var resolved = WindowPlacementRule.Resolve(saved, MinWidth, MinHeight, smallWorkArea, smallWorkArea);

        // Check 2 clamps up to the minimum; check 5 must not clamp it back down below that minimum, even
        // though the work area is smaller than it: the outcome is a window taller than the work area,
        // exactly as MainWindow.SizeToWorkArea already produces today.
        Assert.Equal(MinWidth, resolved!.Value.Width);
        Assert.Equal(MinHeight, resolved.Value.Height);
    }
}
