namespace DigItExplorer.Core.Formats;

/// <summary>Decoder for <c>FX_*.SMP</c> sound effects: headerless signed 8-bit mono PCM at 11,025 Hz.</summary>
public static class SmpAudio
{
    /// <summary>The source's sample rate.</summary>
    public const int SampleRate = 11025;

    /// <summary>The source is mono.</summary>
    public const int Channels = 1;

    /// <summary>Bits per sample of the decoded output (the source is 8-bit; see <see cref="ToPcm16"/>).</summary>
    public const int BitsPerSample = 16;

    /// <summary>Duration of a raw SMP payload (1 byte = 1 sample).</summary>
    public static TimeSpan DurationOf(int byteLength)
        => TimeSpan.FromSeconds((double)byteLength / SampleRate);

    /// <summary>Converts signed 8-bit PCM samples to 16-bit little-endian PCM.</summary>
    public static byte[] ToPcm16(ReadOnlySpan<byte> raw)
    {
        var pcm = new byte[raw.Length * 2];
        for (int i = 0; i < raw.Length; i++)
        {
            short sample = (short)((sbyte)raw[i] << 8);
            pcm[i * 2] = (byte)sample;
            pcm[i * 2 + 1] = (byte)(sample >> 8);
        }
        return pcm;
    }
}
