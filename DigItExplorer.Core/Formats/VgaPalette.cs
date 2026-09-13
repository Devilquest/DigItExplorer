namespace DigItExplorer.Core.Formats;

/// <summary>A 256-color VGA palette scaled from 6-bit (0..63) to 8-bit (0..255) RGB.</summary>
public sealed class VgaPalette
{
    private readonly byte[] _rgb; // 256 × (R,G,B)

    private VgaPalette(byte[] rgb) => _rgb = rgb;

    /// <summary>Builds a palette from a 768-byte 6-bit VGA palette.</summary>
    public static VgaPalette From6Bit(ReadOnlySpan<byte> raw768)
    {
        var rgb = new byte[768];
        for (int i = 0; i < 768; i++)
            rgb[i] = (byte)Math.Min(255, raw768[i] * 255 / 63);
        return new VgaPalette(rgb);
    }

    /// <summary>768 bytes, R,G,B per palette index, 8-bit.</summary>
    public ReadOnlySpan<byte> Rgb => _rgb;

    /// <summary>The 8-bit RGB color for a palette index (0..255).</summary>
    public (byte R, byte G, byte B) this[int index]
        => (_rgb[index * 3], _rgb[index * 3 + 1], _rgb[index * 3 + 2]);
}
