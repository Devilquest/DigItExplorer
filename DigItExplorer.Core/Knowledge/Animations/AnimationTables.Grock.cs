namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Grock enemy animations (33 frames, sprite base 0x384, seg3:0x3B32).</summary>
    public static readonly CharacterAnimSet Grock = new("Grock", 0x10,
    [
        new("fly", Rng(0, 8), AnimMode.Loop, "state 0; seg3:0x3BA1; normal flight/idle"),
        new("throw", Rng(9, 17), AnimMode.Loop, "state 0; seg3:0x3C0D; shooting window"),
        new("turn", Rng(18, 22), AnimMode.Once, "state 1; seg3:0x3C8A; flips facing at end"),
        new("inflate", Rng(23, 31), AnimMode.Once, "state 2; seg3:0x3D1D; pumped by shots"),
        new("deflate", Rev(Rng(23, 31)), AnimMode.Once, "state 2; inflate reversed when not fully pumped"),
        new("fall_away", [32], AnimMode.Pose, "state 3; seg3:0x3D28; knocked away"),
    ], RosterOrdinal: 37);
}
