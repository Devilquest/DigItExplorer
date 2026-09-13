namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Falling rock hazard animations (WO_RCK{suffix}, category 0x14), exposing only the intact
    /// rock: the crumble cells are not decoded, so no sequence is invented for them.</summary>
    public static readonly CharacterAnimSet FallingRock = new("Falling Rock", null,
    [
        new("rock", [0], AnimMode.Pose, "cell 0; the whole rock the map stamps"),
    ],
        WorldSheet: new WorldSheetSet("WO_RCK{0}"),
        SlicedSheet: true);
}
