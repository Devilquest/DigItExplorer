using System.IO;
using System.Text;

namespace DigItExplorer.Core.Export;

/// <summary>Builds the name an exported file is offered under.</summary>
public static class ExportFileName
{
    private static readonly char[] Invalid = Path.GetInvalidFileNameChars();

    /// <summary>Generates a sanitized default export file name for a subject at a specified integer scale.</summary>
    /// <param name="subject">Subject or title string.</param>
    /// <param name="scale">Magnification factor (scale > 1 appends suffix).</param>
    /// <returns>Sanitized filename string without extension.</returns>
    public static string For(string subject, int scale)
    {
        var cleaned = new StringBuilder(subject?.Length ?? 0);
        bool pendingSpace = false;

        foreach (var c in subject ?? string.Empty)
        {
            // Whitespace is decided first, and on the character as it came in: a tab is both whitespace and
            // a control character, so asking whether the file system rejects it first would replace it with
            // an underscore instead of collapsing it away.
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = cleaned.Length > 0; // never leading, and dropped outright if nothing follows
                continue;
            }

            if (pendingSpace)
            {
                cleaned.Append(' ');
                pendingSpace = false;
            }
            cleaned.Append(c == '·' ? '-' : c == '.' || Invalid.Contains(c) ? '_' : c);
        }

        var name = cleaned.Length == 0 ? "export" : cleaned.ToString();
        return scale > 1 ? $"{name}_{scale}x" : name;
    }

    /// <summary>Generates a sanitized export filename for multi-frame horizontal strips.</summary>
    public static string ForStrip(string subject, int scale) => $"{For(subject, scale)}_frames";

    /// <summary>Generates a sanitized filename for a single zero-padded frame within a sequence.</summary>
    public static string ForFrame(string subject, int scale, int frameIndex, int frameCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(frameCount, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(frameIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(frameIndex, frameCount);

        // The scale sits before the number so every frame of one export shares a prefix and they group
        // together in a folder that also holds other exports.
        int width = frameCount.ToString().Length;
        return $"{For(subject, scale)}_{(frameIndex + 1).ToString().PadLeft(width, '0')}";
    }
}
