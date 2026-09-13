namespace DigItExplorer.Core.Knowledge;

/// <summary>A node on a world-map path from the game's node table (DS:0x27A in MAIN.EXE).</summary>
/// <param name="Index">Node index on the map (0..15).</param>
/// <param name="Sign">Node kind (level, checkpoint, lore, gate, unused).</param>
/// <param name="Name">In-game display name for the node.</param>
/// <param name="Gate">Destination world-change target if this node is a gate; otherwise, null.</param>
public sealed record LevelNode(int Index, SignType Sign, string Name, GateTarget? Gate = null)
{
    /// <summary>True for a playable level node (the only kind that has <c>LVLxxx</c> files).</summary>
    public bool IsLevel => Sign == SignType.Level;
}

/// <summary>The far side of a world-change gate: which world's map and which node it lands on.</summary>
/// <param name="World">Destination map/world.</param>
/// <param name="Node">Destination node index on that map.</param>
public readonly record struct GateTarget(World World, int Node);
