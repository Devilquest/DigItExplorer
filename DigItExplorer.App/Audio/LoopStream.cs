using NAudio.Wave;

namespace DigItExplorer.App.Audio;

/// <summary>Interface for audio streams supporting toggleable seamless looping.</summary>
internal interface ILoopingStream
{
    bool EnableLooping { get; set; }
}

/// <summary>WaveStream wrapper that restarts playback from the beginning when reaching stream end.</summary>
internal sealed class LoopStream : WaveStream, ILoopingStream
{
    private readonly WaveStream _source;

    public LoopStream(WaveStream source) => _source = source;

    /// <summary>Whether playback automatically loops back to stream origin.</summary>
    public bool EnableLooping { get; set; }

    public override WaveFormat WaveFormat => _source.WaveFormat;
    public override long Length => _source.Length;

    public override long Position
    {
        get => _source.Position;
        set => _source.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int total = 0;
        while (total < count)
        {
            int read = _source.Read(buffer, offset + total, count - total);
            if (read == 0)
            {
                if (!EnableLooping || _source.Length == 0) break;
                _source.Position = 0;
            }
            total += read;
        }
        return total;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _source.Dispose();
        base.Dispose(disposing);
    }
}
