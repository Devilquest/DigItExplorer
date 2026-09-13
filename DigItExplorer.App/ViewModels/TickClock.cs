using System.Diagnostics;
using System.Windows.Threading;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.ViewModels;

/// <summary>Drives a timed preview: it banks a base tick, measures elapsed time against it, and scales
/// that measurement by the playback speed.</summary>
internal sealed class TickClock
{
    private readonly DispatcherTimer _timer = new(DispatcherPriority.Render);
    private readonly Stopwatch _stopwatch = new();
    private double _frameMs;
    private int _tickCount;

    /// <summary>Raised once per frame interval while the clock runs.</summary>
    public event EventHandler? Tick
    {
        add => _timer.Tick += value;
        remove => _timer.Tick -= value;
    }

    /// <summary>The tick that elapsed time is measured from.</summary>
    public int Base { get; set; }

    /// <summary>Playback speed multiplier applied to elapsed time.</summary>
    public double Speed { get; set; } = 1;

    /// <summary>Whether the end of the loaded item wraps back to its first tick.</summary>
    public bool Loop { get; set; } = true;

    /// <summary>The tick reached now, wrapped into the loaded item's length or held at its last tick.</summary>
    public int Current()
        => PlaybackTick.At(Base, _stopwatch.Elapsed.TotalMilliseconds, Speed, _frameMs, _tickCount, Loop);

    /// <summary>Adopts a newly loaded item's timing and starts running from its first tick.</summary>
    /// <param name="frameMs">Duration of one tick at ×1 speed.</param>
    /// <param name="tickCount">Number of ticks before playback wraps.</param>
    public void Load(double frameMs, int tickCount)
    {
        _frameMs = frameMs;
        _tickCount = tickCount;
        Base = 0;
        _timer.Interval = TimeSpan.FromMilliseconds(frameMs);
        _stopwatch.Restart();
        _timer.Start();
    }

    /// <summary>Resumes from the banked base tick.</summary>
    public void Resume()
    {
        _stopwatch.Restart();
        _timer.Start();
    }

    /// <summary>Stops running, banking the tick reached so a later resume continues from it.</summary>
    public void Pause()
    {
        Base = Current();
        _timer.Stop();
        _stopwatch.Reset();
    }

    /// <summary>Stops running and forgets the loaded item's timing.</summary>
    public void Unload()
    {
        _timer.Stop();
        _stopwatch.Reset();
        Base = 0;
        _tickCount = 0;
    }

    /// <summary>Restarts the elapsed measurement without moving the base tick, for a caller that has
    /// just set <see cref="Base"/> itself.</summary>
    /// <param name="running">Whether playback is active and the measurement should resume immediately.</param>
    public void RestartElapsed(bool running)
    {
        _stopwatch.Reset();
        if (running) _stopwatch.Start();
    }

    /// <summary>Banks the tick reached and restarts the measurement, so a speed change applies from here
    /// instead of rescaling the time already elapsed.</summary>
    /// <param name="running">Whether playback is active and the measurement should resume immediately.</param>
    public void Rebase(bool running)
    {
        Base = Current();
        RestartElapsed(running);
    }
}
