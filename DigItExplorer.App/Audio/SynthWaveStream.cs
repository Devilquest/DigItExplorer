using System.Runtime.InteropServices;
using DigItExplorer.Core.Audio;
using NAudio.Wave;

namespace DigItExplorer.App.Audio;

/// <summary>WaveStream adapter over LdsSynthesizer providing single-pass timeline navigation and loop crossfading.</summary>
internal sealed class SynthWaveStream(LdsSynthesizer synth) : WaveStream, ILoopingStream
{
    private long _physical;     // Current physical sample byte offset.
    private int _fadeDone = -1; // Number of crossfade samples emitted (-1 when inactive).
    private bool _inWarmPass;   // Whether playback is currently within a subsequent loop iteration.

    public bool EnableLooping { get; set; }

    /// <summary>Physical synthesis sample index used for audio buffering indicators.</summary>
    public long PhysicalSamplePosition => _physical / 2;

    private long WrapEndBytes => (synth.Loops ? synth.LoopEndSample : synth.TotalSamples) * 2;
    private long PassBytes => synth.Loops ? synth.LoopStartSample * 2 : WrapEndBytes;
    private long BodyBytes => WrapEndBytes - (synth.Loops ? synth.LoopStartSample * 2 : 0);

    public override WaveFormat WaveFormat { get; } = new(synth.SampleRate, 16, 1);

    public override long Length => PassBytes;

    public override long Position
    {
        get => Math.Clamp(_physical < PassBytes ? _physical : _physical - BodyBytes, 0, PassBytes);
        set
        {
            long logical = Math.Clamp(value & ~1L, 0, PassBytes);
            // Maintain warm-pass mapping when seeking within active loop region.
            if (synth.Loops && _inWarmPass && logical >= PassBytes - BodyBytes)
            {
                _physical = logical + BodyBytes;
            }
            else
            {
                _physical = logical;
                _inWarmPass = false;
            }
            _fadeDone = -1; // Abandon in-progress wrap crossfade on seek.
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var destination = MemoryMarshal.Cast<byte, short>(buffer.AsSpan(offset, count & ~1));
        int total = 0;
        while (total < destination.Length)
        {
            int n = ReadCore(destination[total..]);
            if (n == 0) break;
            total += n;
        }
        return total * 2;
    }

    private int ReadCore(Span<short> destination)
    {
        // Reset crossfade if looping is disabled mid-fade.
        if (_fadeDone >= 0 && (!EnableLooping || !synth.Loops))
            _fadeDone = -1;
        if (_fadeDone >= 0)
        {
            int fade = LdsSynthesizer.LoopCrossfadeSamples;
            int n = Math.Min(destination.Length, fade - _fadeDone);
            if (n > 0)
            {
                Span<short> body = stackalloc short[512];
                Span<short> tail = stackalloc short[512];
                int written = 0;
                while (written < n)
                {
                    int chunk = Math.Min(512, n - written);
                    int got = synth.Read(_physical / 2, body[..chunk]);
                    int gotTail = synth.Read(synth.LoopEndSample + _fadeDone + written, tail[..chunk]);
                    if (got == 0) break;
                    for (int i = 0; i < got; i++)
                    {
                        double w = (_fadeDone + written + i + 1) / (double)fade;
                        double t = i < gotTail ? tail[i] : 0;
                        destination[written + i] = (short)(body[i] * w + t * (1 - w));
                    }
                    written += got;
                    _physical += got * 2L;
                }
                _fadeDone += written;
                if (_fadeDone >= fade) _fadeDone = -1;
                if (written > 0) return written;
            }
            _fadeDone = -1;
        }

        // Finish active pass when looping is disabled; warm loop wraps to body start.
        long limit = EnableLooping || _inWarmPass ? WrapEndBytes : PassBytes;
        if (_physical >= limit)
        {
            if (!EnableLooping || BodyBytes <= 0) return 0;
            _physical = WrapEndBytes - BodyBytes;
            _inWarmPass = true;
            if (synth.Loops) { _fadeDone = 0; return ReadCore(destination); }
        }

        int want = (int)Math.Min(destination.Length, (limit - _physical) / 2);
        if (want <= 0) return 0;
        int samples = synth.Read(_physical / 2, destination[..want]);
        _physical += samples * 2L;
        // Flag warm-pass playback when advancing past the first iteration.
        if (synth.Loops && EnableLooping && _physical > PassBytes) _inWarmPass = true;
        return samples;
    }
}
