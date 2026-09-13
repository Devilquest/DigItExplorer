using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Formats user-facing diagnostic messages for damaged or defective game files.</summary>
public static class DamageNote
{
    /// <summary>What a damage warning puts between its own text and the repair tool's name.</summary>
    public const string RepairLead = " ";

    /// <summary>What a damage warning puts after the repair tool's name.</summary>
    public const string RepairEnd = " can fix it.";

    /// <summary>Returns a diagnostic warning for a sheet decode defect, or null if clean.</summary>
    public static string? For(SheetDefect defect) => defect switch
    {
        SheetDefect.None => null,
        SheetDefect.NotASheet => "This file is too short to hold an image.",
        SheetDefect.ImpossibleFrameCount => "This sheet's header is invalid. The image itself "
            + "decodes, but its colors may be wrong because the sheet's own palette sits immediately "
            + "after the header.",
        SheetDefect.TruncatedChain => "The image data stops before the file does.",
        _ => "The compressed image data is not valid.",
    };

    /// <summary>Returns a diagnostic warning for missing or incomplete map layers, or null if complete.</summary>
    public static string? ForLayers(params (string Layer, int Missing, int Blocks)[] layers)
    {
        var damaged = layers.Where(l => l.Missing > 0).ToList();
        if (damaged.Count == 0) return null;

        var sentences = damaged.Select((l, i) =>
            $"The {l.Layer} layer is missing {l.Missing} of its {l.Blocks} blocks");
        return string.Join(". ", sentences) + ".";
    }
}
