namespace DigItExplorer.App.ViewModels;

/// <summary>The speed choices every transport bar offers, shared so the menu labels cannot drift
/// from the multipliers they select.</summary>
internal static class PlaybackSpeeds
{
    // The labels are listed rather than formatted: the set mixes one and two decimal places
    // (0.10 against 0.5), which no single format string reproduces.
    private static readonly (double Multiplier, string Label)[] Entries =
    [
        (0.10, "×0.10"),
        (0.25, "×0.25"),
        (0.5, "×0.5"),
        (1, "×1"),
        (2, "×2"),
        (4, "×4"),
    ];

    /// <summary>Index of the ×1 entry, which every player starts on.</summary>
    public const int DefaultIndex = 3;

    /// <summary>Speed multipliers in menu order, indexed by the transport's selected index.</summary>
    public static readonly double[] Multipliers = [.. Entries.Select(e => e.Multiplier)];

    /// <summary>Menu labels in the same order, for the transport speed pickers to bind to.</summary>
    public static readonly IReadOnlyList<string> Labels = [.. Entries.Select(e => e.Label)];
}
