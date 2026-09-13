/*
 * Part of the Core/Audio LGPL-2.1 module (see LICENSE in this folder): drives the LdsPlayer
 * sequencer (ported from AdPlug, LGPL-2.1) through the NukedOpl OPL3 core (LGPL-2.1) to render
 * a whole LDS tune into an in-RAM PCM buffer.
 */

namespace DigItExplorer.Core.Audio;

/// <summary>A fully synthesized LDS tune: mono 16-bit PCM plus what the sequencer learned about it.</summary>
public sealed record LdsRenderedTrack(short[] Pcm, int SampleRate, TimeSpan Duration, bool Loops)
{
    /// <summary>The PCM as little-endian bytes, ready for a playback stream.</summary>
    public byte[] ToPcmBytes()
    {
        var bytes = new byte[Pcm.Length * 2];
        Buffer.BlockCopy(Pcm, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}

/// <summary>Blocking, whole-tune LDS renderer built on <see cref="LdsSynthesizer"/>, for the interactive
/// case (playback, seeking while rendering continues in the background) use the synthesizer directly.</summary>
public static class LdsRenderer
{
    /// <summary>The sample rate used when the caller doesn't need a specific one.</summary>
    public const int DefaultSampleRate = 44100;

    /// <summary>Safety cap on the length probe: far above the longest game tune (TUNE9, 4:50).</summary>
    private static readonly TimeSpan MaxDuration = TimeSpan.FromMinutes(15);

    /// <summary>Probes a tune's duration and looping property without FM synthesis.</summary>
    /// <param name="data">The raw LDS file bytes.</param>
    /// <param name="duration">Outputs the measured duration of the track.</param>
    /// <param name="loops">Outputs whether the track loops.</param>
    /// <returns><c>true</c> if the LDS module was parsed successfully; otherwise, <c>false</c>.</returns>
    public static bool TryProbe(byte[] data, out TimeSpan duration, out bool loops)
    {
        duration = TimeSpan.Zero;
        loops = false;
        if (!LdsPlayer.TryLoad(data, out var player)) return false;

        long ticks = 0;
        long maxTicks = (long)(MaxDuration.TotalSeconds * player!.RefreshRate);
        while (player.Update() && ticks < maxTicks) ticks++;
        ticks++; // the ending tick itself still occupies one refresh interval

        duration = TimeSpan.FromSeconds(ticks / player.RefreshRate);
        loops = player.SongLooped;
        return true;
    }

    /// <summary>Renders a full LDS tune to mono 16-bit PCM in a blocking call.</summary>
    /// <param name="data">The raw LDS file bytes.</param>
    /// <param name="sampleRate">Target output sample rate in Hz.</param>
    /// <param name="cancellationToken">Cancellation token for aborting the render.</param>
    /// <returns>An <see cref="LdsRenderedTrack"/> containing the PCM data, or <c>null</c> if invalid.</returns>
    public static LdsRenderedTrack? Render(byte[] data, int sampleRate = DefaultSampleRate,
                                           CancellationToken cancellationToken = default)
    {
        var synth = LdsSynthesizer.TryCreate(data, sampleRate);
        if (synth is null) return null;

        synth.EnsureRendered(synth.TotalSamples, cancellationToken);
        return new LdsRenderedTrack(synth.GetPcm(), sampleRate, synth.Duration, synth.Loops);
    }
}
