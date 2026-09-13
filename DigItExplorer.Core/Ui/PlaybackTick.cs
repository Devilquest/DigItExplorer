namespace DigItExplorer.Core.Ui;

/// <summary>Locates where a timed preview has reached, from the tick it resumed at and the time since.</summary>
public static class PlaybackTick
{
    /// <summary>Advances a base tick by however many whole frames the elapsed time covers at the given
    /// speed, wrapping back to zero at the end of the sequence or stopping on its last tick.</summary>
    /// <param name="baseTick">The tick playback resumed from.</param>
    /// <param name="elapsedMs">Real time since it resumed, in milliseconds.</param>
    /// <param name="speed">Playback speed multiplier applied to the elapsed time.</param>
    /// <param name="frameMs">Duration of one tick at ×1 speed.</param>
    /// <param name="tickCount">Number of ticks in the sequence.</param>
    /// <param name="loop">Whether the end of the sequence wraps back to its first tick.</param>
    /// <returns>The current tick, or 0 when nothing is loaded.</returns>
    public static int At(int baseTick, double elapsedMs, double speed, double frameMs, int tickCount,
        bool loop = true)
    {
        if (tickCount <= 0) return 0;
        double scaledMs = elapsedMs * speed;
        int reached = baseTick + (int)(scaledMs / frameMs);
        return loop ? reached % tickCount : Math.Min(reached, tickCount - 1);
    }
}
