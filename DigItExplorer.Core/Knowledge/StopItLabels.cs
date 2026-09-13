using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Core.Knowledge;

/// <summary>Reel labels and ink styles for the Stop It! minigame parsed from <c>MAIN.EXE</c>.</summary>
public sealed class StopItLabels
{
    private StopItLabels(byte[] focused, byte[] spinning, int focusedStyle, int unfocusedStyle)
    {
        Focused = focused;
        Spinning = spinning;
        FocusedStyle = focusedStyle;
        UnfocusedStyle = unfocusedStyle;
    }

    /// <summary>Label text for the reel currently holding player focus (raw CP437 bytes).</summary>
    public byte[] Focused { get; }

    /// <summary>Label text for unfocused spinning reels (raw CP437 bytes).</summary>
    public byte[] Spinning { get; }

    /// <summary>The <see cref="Formats.GameFont.Ramp"/> style the focused reel's label is drawn in.</summary>
    public int FocusedStyle { get; }

    /// <summary>The <see cref="Formats.GameFont.Ramp"/> style every unfocused reel's label is drawn in.</summary>
    public int UnfocusedStyle { get; }

    /// <summary>Parses reel label strings and ink style indices from the executable.</summary>
    internal static bool TryRead(ExeReader reader, out StopItLabels labels)
    {
        labels = null!;

        if (!reader.TryReadPascalString(ExeLayout.StopItFocusedLabel, out var focused) || focused.Length == 0)
            return false;
        if (!reader.TryReadPascalString(ExeLayout.StopItSpinningLabel, out var spinning) || spinning.Length == 0)
            return false;
        if (!TryReadStyle(reader, ExeLayout.StopItFocusedStyleSite, out int focusedStyle)) return false;
        if (!TryReadStyle(reader, ExeLayout.StopItUnfocusedStyleSite, out int unfocusedStyle)) return false;

        labels = new StopItLabels(focused, spinning, focusedStyle, unfocusedStyle);
        return true;
    }

    private static bool TryReadStyle(ExeReader reader, ExeAddress site, out int style)
    {
        if (!reader.TryReadPushedWord(site, out style, out _)) return false;
        return style is >= 0 and <= Formats.GameFont.MaxRampStyle;
    }
}
