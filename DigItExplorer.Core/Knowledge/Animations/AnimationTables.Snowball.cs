namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Snowball throw projectile animations (WO_THR02, sprite base 850+29).</summary>
    public static readonly CharacterAnimSet Snowball = new("Snowball", null,
    [
        new("snowball", [0], AnimMode.Pose, "band cell 0; sprite 850+29"),
        new("snowball_pile", [1], AnimMode.Pose, "band cell 1; no seg3:0x7C4A call site references it",
            DisplayName: "Snowball pile (unused)"),
    ],
        WorldSheet: new WorldSheetSet("WO_THR{0}"),
        SlicedSheet: true,
        SliceSeparator: 150,
        SliceBand: (180, 199));
}
