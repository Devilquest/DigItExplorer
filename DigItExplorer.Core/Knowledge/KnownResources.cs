namespace DigItExplorer.Core.Knowledge;

/// <summary>Known facts about specific game resources, unused assets, and duplicate audio files.</summary>
public static class KnownResources
{
    /// <summary>List of known unused asset files shipped in the game archives.</summary>
    public static readonly IReadOnlyList<string> Unused =
    [
        "DUG5A.SPF", "DUG5B.SPF", "DUG5C.SPF", "DUG5D.SPF",
        "WO_SNL03.SPF", "WO_RED00.MPF", "WO_RED02.MPF", "WO_POP00.MPF",
        "DND.SPF", "SPRING.SPF", "MANLOGO.ANI", "INTRO00.MPF",
        "J_GOVER.DAT", "J_SAD2.DAT", "J_LONG.DAT", "J_LOUD.DAT",
        "J_LOOP.DAT", "J_LOOP2.DAT", "J_LOOP3.DAT", "J_BONUS.DAT", "J_SUPERM.DAT",
        "GPALFIX.DAT",
    ];

    /// <summary>Maps unused J_* jingle filenames to their byte-identical TUNE1x level music equivalents.</summary>
    public static readonly IReadOnlyDictionary<string, string> AudioDuplicateOf =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["J_LOOP3.DAT"] = "TUNE10.DAT",
            ["J_BONUS.DAT"] = "TUNE11.DAT",
            ["J_SUPERM.DAT"] = "TUNE12.DAT",
            ["J_LOOP.DAT"] = "TUNE13.DAT",
            ["J_LOOP2.DAT"] = "TUNE14.DAT",
        };
}
