namespace DigItExplorer.Core.Knowledge;

/// <summary>Signpost types and node kinds in the world map node table.</summary>
public enum SignType
{
    /// <summary>Dug head: a playable level (has <c>LVLxxx</c> files).</summary>
    Level = 0,

    /// <summary>Blue check: a checkpoint / save spot (no level files).</summary>
    Checkpoint = 1,

    /// <summary>Yellow star: a "Traces of Dugette" lore spot (no level files).</summary>
    Trace = 2,

    /// <summary>Purple footprints: a world-change gate; <see cref="LevelNode.Gate"/> holds the destination.</summary>
    Gate = 3,

    /// <summary>Red Draggo: referenced by no node in any shipped map (unused).</summary>
    Draggo = 4,
}
