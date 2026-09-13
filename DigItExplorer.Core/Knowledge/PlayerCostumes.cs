namespace DigItExplorer.Core.Knowledge;

/// <summary>A player costume definition including sheet filename code, world, and representative palette.</summary>
/// <param name="Code">Costume character code embedded in sprite filenames (e.g. '0', '6', '3', '5').</param>
/// <param name="World">World where the costume is worn, or null for unused costumes.</param>
/// <param name="Palette">Representative palette resource filename.</param>
public readonly record struct PlayerCostume(char Code, World? World, string Palette);

/// <summary>Definitions and world mapping for Player costume sheets (DUG{Code}{letter}.SPF).</summary>
public static class PlayerCostumes
{
    /// <summary>All known player costumes: '0' (Caves/Water), '6' (Snow), '3' (Underworld), '5' (Unused).</summary>
    public static readonly IReadOnlyList<PlayerCostume> All =
    [
        new('0', World.Caves, "LVL000.PAL"),
        new('6', World.Snow, "LVL400.PAL"),
        new('3', World.Underworld, "LVL600.PAL"),
        new('5', null, "LVL000.PAL"),
    ];

    /// <summary>Returns the costume code character for a given world.</summary>
    public static char CodeFor(World world)
    {
        foreach (var costume in All)
            if (costume.World == world) return costume.Code;
        return '0';
    }
}
