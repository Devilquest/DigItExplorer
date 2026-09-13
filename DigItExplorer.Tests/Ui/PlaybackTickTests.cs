using DigItExplorer.Core.Ui;

namespace DigItExplorer.Tests;

public class PlaybackTickTests
{
    [Fact]
    public void No_elapsed_time_leaves_playback_on_the_tick_it_resumed_from()
    {
        Assert.Equal(7, PlaybackTick.At(baseTick: 7, elapsedMs: 0, speed: 1, frameMs: 100, tickCount: 20));
    }

    [Fact]
    public void A_partial_frame_does_not_advance_the_tick()
    {
        Assert.Equal(0, PlaybackTick.At(0, elapsedMs: 99, speed: 1, frameMs: 100, tickCount: 20));
        Assert.Equal(1, PlaybackTick.At(0, elapsedMs: 100, speed: 1, frameMs: 100, tickCount: 20));
    }

    [Fact]
    public void Speed_scales_the_elapsed_time_rather_than_the_frame_duration()
    {
        Assert.Equal(4, PlaybackTick.At(0, elapsedMs: 100, speed: 4, frameMs: 100, tickCount: 20));
        Assert.Equal(0, PlaybackTick.At(0, elapsedMs: 100, speed: 0.10, frameMs: 100, tickCount: 20));
        Assert.Equal(1, PlaybackTick.At(0, elapsedMs: 1000, speed: 0.10, frameMs: 100, tickCount: 20));
    }

    [Fact]
    public void Playback_wraps_at_the_end_of_the_sequence()
    {
        Assert.Equal(0, PlaybackTick.At(0, elapsedMs: 500, speed: 1, frameMs: 100, tickCount: 5));
        Assert.Equal(1, PlaybackTick.At(0, elapsedMs: 600, speed: 1, frameMs: 100, tickCount: 5));
    }

    [Fact]
    public void Playback_that_is_not_looping_holds_the_last_tick_instead_of_wrapping()
    {
        Assert.Equal(4, PlaybackTick.At(0, elapsedMs: 400, speed: 1, frameMs: 100, tickCount: 5, loop: false));
        Assert.Equal(4, PlaybackTick.At(0, elapsedMs: 500, speed: 1, frameMs: 100, tickCount: 5, loop: false));
    }

    [Fact]
    public void Time_covering_several_frames_at_once_does_not_step_over_the_non_looping_end()
    {
        // What a late timer firing does: one gap crosses the last tick, which an exact end test misses.
        Assert.Equal(4, PlaybackTick.At(baseTick: 3, elapsedMs: 250, speed: 1, frameMs: 100, tickCount: 5, loop: false));
        Assert.Equal(4, PlaybackTick.At(baseTick: 0, elapsedMs: 300, speed: 4, frameMs: 100, tickCount: 5, loop: false));
    }

    [Fact]
    public void An_empty_sequence_reports_the_first_tick_when_not_looping_as_well()
    {
        Assert.Equal(0, PlaybackTick.At(3, elapsedMs: 500, speed: 1, frameMs: 100, tickCount: 0, loop: false));
    }

    [Fact]
    public void The_base_tick_is_included_in_the_wrap()
    {
        Assert.Equal(1, PlaybackTick.At(baseTick: 4, elapsedMs: 200, speed: 1, frameMs: 100, tickCount: 5));
    }

    [Fact]
    public void An_empty_sequence_reports_the_first_tick_rather_than_dividing_by_its_length()
    {
        Assert.Equal(0, PlaybackTick.At(3, elapsedMs: 500, speed: 1, frameMs: 100, tickCount: 0));
    }

    [Fact]
    public void Rebasing_on_a_speed_change_keeps_the_position_already_reached()
    {
        // What TickClock.Rebase does: bank the tick reached at the old speed, then measure afresh.
        int banked = PlaybackTick.At(0, elapsedMs: 250, speed: 1, frameMs: 100, tickCount: 20);
        Assert.Equal(2, banked);

        // The new speed applies only to time from here, so the first 100 ms at x4 adds four ticks.
        Assert.Equal(6, PlaybackTick.At(banked, elapsedMs: 100, speed: 4, frameMs: 100, tickCount: 20));
    }
}
