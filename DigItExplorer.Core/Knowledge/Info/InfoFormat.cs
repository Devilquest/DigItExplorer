using System.Globalization;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Formats values for display in the Info panel.</summary>
internal static class InfoFormat
{
    private const int Kibibyte = 1024;

    /// <summary>Formats a byte count into a human-readable size string (B or KB).</summary>
    public static string Bytes(long count)
        => count < Kibibyte
            ? $"{count} B"
            : $"{(count / (double)Kibibyte).ToString("0.#", CultureInfo.InvariantCulture)} KB";

    /// <summary>Formats duration in seconds into standard time text.</summary>
    public static string Duration(double seconds)
        => seconds >= 60
            ? $"{(int)(seconds / 60)}:{(int)(seconds % 60):D2} min"
            : string.Create(CultureInfo.InvariantCulture, $"{seconds:0.00} s");

    /// <summary>Formats an audio sample rate in Hz.</summary>
    public static string SampleRate(int hz)
        => $"{hz.ToString("N0", CultureInfo.InvariantCulture)} Hz";
}

