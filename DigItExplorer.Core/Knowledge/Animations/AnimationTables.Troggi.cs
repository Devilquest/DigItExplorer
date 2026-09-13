namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Troggi enemy animations (29 frames, sprite base 0x352, seg3:0x524C).</summary>
    public static readonly CharacterAnimSet Troggi = new("Troggi", 0x0F,
    [
        new("throw", Rng(0, 13), AnimMode.Loop, "state 0; aim-hold 0, throws at frame 9"),
        new("inflate", Rng(14, 22), AnimMode.Once, "state 1; pumped by shots"),
        new("deflate", Rev(Rng(14, 22)), AnimMode.Once, "state 1; inflate reversed when not fully pumped"),
        new("fall_away", [23], AnimMode.Pose, "state 3; knocked away"),
        new("turn", Rng(24, 28), AnimMode.Once, "state 2; flips facing at end"),
    ], RosterOrdinal: 32);
}
