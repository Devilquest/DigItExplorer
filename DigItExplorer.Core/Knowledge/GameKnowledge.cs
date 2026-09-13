using System.Diagnostics.CodeAnalysis;

namespace DigItExplorer.Core.Knowledge;

/// <summary>Provides level file parsing, world prefix mapping, and audio filename resolution conventions.</summary>
public static class GameKnowledge
{
    /// <summary>The four playable worlds plus the boss screen, in progression order.</summary>
    public static readonly IReadOnlyList<World> Worlds =
        [World.Caves, World.Water, World.Snow, World.Underworld, World.Boss];

    /// <summary>The world-map file prefix (MAP00 to MAP04) whose resources belong to this world.</summary>
    public static string WorldMapPrefix(World world) => $"MAP{(int)world:D2}";

    /// <summary>Resolves a bare tune name to its corresponding .DAT audio filename.</summary>
    public static string TuneFile(string tuneName) => $"{tuneName}.DAT";

    /// <summary>The music file a tune <b>selector</b> resolves to: the form the engine builds when a
    /// header picks its music by number rather than naming it.</summary>
    public static string TuneFile(int tuneNo) => TuneFile($"TUNE{tuneNo}");

    // ---- File → (world, node, substage) mapping -----------------------------

    /// <summary>Parses a level filename (LVL{D1}{D2}{D3}) into its world, node index, and substage slot.</summary>
    /// <param name="fileName">The filename to parse.</param>
    /// <param name="world">Outputs the mapped <see cref="World"/>.</param>
    /// <param name="node">Outputs the 0-based node index.</param>
    /// <param name="substage">Outputs the substage index.</param>
    /// <returns><c>true</c> if valid level filename digits were parsed; otherwise, <c>false</c>.</returns>
    public static bool TryParseLevelFile(string fileName, out World world, out int node, out int substage)
    {
        world = default; node = 0; substage = 0;
        if (!TryGetLevelDigits(fileName, out int d1, out int d2, out int d3))
            return false;

        switch (d1)
        {
            case 0: world = World.Caves; break;
            case 2: world = World.Water; break;
            case 4 or 5: world = World.Snow; break;
            case 6 or 7: world = World.Underworld; break;
            default: return false; // 9 = menu (handled separately), others unused
        }

        node = d1 is 5 or 7 ? 10 + d2 : d2;
        substage = d3;
        return true;
    }

    /// <summary>True for a playable level file (<c>LVL0/2/4/5/6/7xx</c>), i.e. anything <see cref="TryParseLevelFile"/> accepts.</summary>
    public static bool IsLevelFile(string fileName) => TryParseLevelFile(fileName, out _, out _, out _);

    /// <summary>True for the main-menu pseudo-level (<c>LVL9xx</c>, e.g. <c>LVL900.DLF</c>): built on the level format but not a world level.</summary>
    public static bool IsMenuFile(string fileName) =>
        TryGetLevelDigits(fileName, out int d1, out _, out _) && d1 == 9;

    /// <summary>Determines if a filename is a world-map resource (MAP00..MAP04) and resolves its world.</summary>
    public static bool TryGetWorldMapFile(string fileName, out World world)
    {
        world = default;
        if (fileName.Length >= 5 && fileName.StartsWith("MAP0", StringComparison.OrdinalIgnoreCase)
                                 && fileName[4] is >= '0' and <= '4')
        {
            world = (World)(fileName[4] - '0');
            return true;
        }
        return false;
    }

    /// <summary>The <c>LVLxxx</c> stem (world + node + substage, no suffix/extension) that groups a level's files, e.g. <c>LVL090F.MPF</c> → <c>LVL090</c>.</summary>
    public static string? LevelStem(string fileName) =>
        TryGetLevelDigits(fileName, out int d1, out int d2, out int d3) ? $"LVL{d1}{d2}{d3}" : null;

    // ---- internals ----------------------------------------------------------

    /// <summary>Reads the three <c>LVL</c> filename digits; false if the name is not <c>LVL</c> + 3 digits.</summary>
    private static bool TryGetLevelDigits(string fileName, out int d1, out int d2, out int d3)
    {
        d1 = d2 = d3 = 0;
        if (fileName.Length < 6 || !fileName.StartsWith("LVL", StringComparison.OrdinalIgnoreCase))
            return false;
        char c1 = fileName[3], c2 = fileName[4], c3 = fileName[5];
        if (c1 is < '0' or > '9' || c2 is < '0' or > '9' || c3 is < '0' or > '9')
            return false;
        d1 = c1 - '0'; d2 = c2 - '0'; d3 = c3 - '0';
        return true;
    }
}
