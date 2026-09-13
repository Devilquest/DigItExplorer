/*
 * Part of the Core/Audio LGPL-2.1 module (see LICENSE in this folder): drives the LdsPlayer
 * sequencer (ported from AdPlug, LGPL-2.1) through the NukedOpl OPL3 core (LGPL-2.1).
 */

using NukedOpl;

namespace DigItExplorer.Core.Audio;

/// <summary>On-demand progressive LDS synthesizer that renders audio incrementally during playback.</summary>
public sealed class LdsSynthesizer
{
    private readonly LdsPlayer player;
    private readonly Opl3 opl3 = new();
    private readonly Opl3Chip chip = new();
    private readonly short[] cache;
    private readonly double samplesPerTick;
    private readonly object gate = new();
    private long rendered;      // samples synthesized so far (guarded by gate)
    private double pending;     // fractional sample carry between ticks (guarded by gate)
    private long renderedTicks; // sequencer ticks advanced so far (guarded by gate)
    private bool done;          // sequencer finished (stop command or full loop region rendered)
    private volatile bool aborted;
    private readonly long loopStartTick;   // 0 for one-shots
    private readonly long loopEndTick;     // second loop-jump execution (or the 0xFC tick)
    private readonly long totalTicksTarget; // loopEndTick + crossfade tail for loopers
    private long loopStartSample;          // exact once rendering passes the loop start
    private long loopEndSample;            // exact once rendering passes the loop end

    /// <summary>Length of the wrap crossfade the playback stream applies (~23 ms at 44.1 kHz).</summary>
    public const int LoopCrossfadeSamples = 1024;

    /// <summary>The output sample rate this synthesizer was created with.</summary>
    public int SampleRate { get; }

    /// <summary>True if the tune loops (a backward/self 0xF9 jump ran during the probe pass).</summary>
    public bool Loops { get; }

    /// <summary>Total synthesized cache duration (intro plus two loop passes for seamless crossfade).</summary>
    public TimeSpan Duration { get; }

    /// <summary>Logical single-pass playback duration before looping or ending.</summary>
    public TimeSpan SinglePassDuration
        => TimeSpan.FromSeconds(Loops ? (double)LoopStartSample / SampleRate : Duration.TotalSeconds);

    /// <summary>Length of the physical PCM cache in samples, including any crossfade tail.</summary>
    public long TotalSamples { get; }

    /// <summary>How far synthesis has progressed: lets a UI show a buffering state when a seek
    /// lands beyond it (the chip state is serial: a far seek must synthesize its way there).</summary>
    public long RenderedSamples { get { lock (gate) return rendered; } }

    /// <summary>Sample offset where the loop body begins after the first loop jump.</summary>
    public long LoopStartSample { get { lock (gate) return loopStartSample; } }

    /// <summary>Sample offset where the second loop pass completes before the crossfade tail.</summary>
    public long LoopEndSample { get { lock (gate) return loopEndSample; } }

    /// <summary>Safety cap: far above the longest game tune (TUNE9, 4:50).</summary>
    private static readonly TimeSpan MaxDuration = TimeSpan.FromMinutes(15);

    private sealed class NukedOplPort(Opl3 opl3, Opl3Chip chip, int sampleRate) : IOplPort
    {
        public void Reset() => opl3.Reset(chip, sampleRate);

        // Space OPL2 register writes >= 2 samples apart to emulate hardware timing and envelope retriggering.
        public void Write(int register, byte value) => opl3.WriteRegBuffered(chip, register, value);
    }

    /// <summary>Initializes an on-demand synthesizer from raw LDS module bytes.</summary>
    /// <param name="data">The raw LDS file bytes.</param>
    /// <param name="sampleRate">Target output sample rate in Hz.</param>
    /// <returns>A new <see cref="LdsSynthesizer"/> instance, or <c>null</c> if parsing fails.</returns>
    public static LdsSynthesizer? TryCreate(byte[] data, int sampleRate = LdsRenderer.DefaultSampleRate)
    {
        // Probe loop bounds: loopers run two passes so the cache contains intro + full warm loop body.
        if (!LdsPlayer.TryLoad(data, out var probe)) return null;
        long maxTicks = (long)(MaxDuration.TotalSeconds * probe!.RefreshRate);
        long ticks = 0, loopStartTick = 0;
        while (ticks < maxTicks)
        {
            probe.Update();
            ticks++;
            if (!probe.Playing) break; // one-shot ended (0xFC) on this tick
            if (probe.SongLooped)
            {
                if (loopStartTick == 0) loopStartTick = ticks; // first jump: end of pass 1
                if (probe.LoopPassCount >= 2) break;           // second jump: one full loop body rendered
            }
        }

        LdsPlayer.TryLoad(data, out var player); // fresh sequencer for the actual rendering
        return new LdsSynthesizer(player!, sampleRate, ticks, loopStartTick, probe.SongLooped);
    }

    private LdsSynthesizer(LdsPlayer player, int sampleRate, long endTick, long loopStartTick, bool loops)
    {
        this.player = player;
        SampleRate = sampleRate;
        Loops = loops;
        this.loopStartTick = loopStartTick;
        loopEndTick = endTick;
        samplesPerTick = sampleRate / player.RefreshRate;

        // Synthesize a short crossfade tail past the loop boundary to prevent phase discontinuity clicks.
        totalTicksTarget = loops
            ? endTick + (long)Math.Ceiling(LoopCrossfadeSamples / samplesPerTick) + 1
            : endTick;

        Duration = TimeSpan.FromSeconds(endTick / player.RefreshRate);
        TotalSamples = (long)(totalTicksTarget * samplesPerTick);
        loopStartSample = (long)(loopStartTick * samplesPerTick); // refined to the exact value during rendering
        loopEndSample = (long)(loopEndTick * samplesPerTick);     // idem
        cache = new short[TotalSamples];
        player.Rewind(new NukedOplPort(opl3, chip, sampleRate));
    }

    /// <summary>Synthesizes audio forward until the requested sample index is rendered.</summary>
    /// <param name="upToSample">Target sample index to render up to.</param>
    /// <param name="cancellationToken">Cancellation token to abort rendering.</param>
    public void EnsureRendered(long upToSample, CancellationToken cancellationToken = default)
    {
        upToSample = Math.Min(upToSample, TotalSamples);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (gate)
            {
                if (rendered >= upToSample || done) return;
                if (aborted)
                {
                    // finalize: the unrendered tail stays silent (cache is zero-initialized)
                    rendered = TotalSamples;
                    done = true;
                    return;
                }
                RenderTick();
            }
        }
    }

    /// <summary>Reads cached samples into destination, synthesizing missing samples on demand.</summary>
    /// <param name="samplePosition">Starting sample offset to read from.</param>
    /// <param name="destination">Target buffer for output PCM samples.</param>
    /// <returns>The number of samples written to destination.</returns>
    public int Read(long samplePosition, Span<short> destination)
    {
        if (samplePosition >= TotalSamples || samplePosition < 0) return 0;
        int count = (int)Math.Min(destination.Length, TotalSamples - samplePosition);
        EnsureRendered(samplePosition + count);
        lock (gate)
        {
            cache.AsSpan((int)samplePosition, count).CopyTo(destination);
        }
        return count;
    }

    /// <summary>Makes any in-progress or future synthesis finish immediately (silent tail):
    /// call before disposing the playback engine so its thread can't stay stuck rendering.</summary>
    public void Abort() => aborted = true;

    /// <summary>Retrieves the underlying PCM sample buffer.</summary>
    internal short[] GetPcm() => cache;

    private void RenderTick()
    {
        player.Update();

        pending += samplesPerTick;
        int samples = (int)pending;
        pending -= samples;

        Span<short> pair = stackalloc short[2];
        for (int i = 0; i < samples && rendered < TotalSamples; i++)
        {
            // OPL2 compatibility mode: keep single mono channel.
            opl3.GenerateResampled(chip, pair);
            cache[rendered++] = pair[0];
        }

        renderedTicks++;
        if (renderedTicks == loopStartTick)
            loopStartSample = rendered; // exact wrap target: right after the loop jump's own tick
        if (renderedTicks == loopEndTick)
            loopEndSample = rendered;   // exact splice boundary

        // Stop on 0xFC opcode or when target ticks (including crossfade tail) are rendered.
        if (!player.Playing || renderedTicks >= totalTicksTarget)
        {
            done = true;
            rendered = TotalSamples; // any tail beyond the last tick stays silent
        }
    }
}
