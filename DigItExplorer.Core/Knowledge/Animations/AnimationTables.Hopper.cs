namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Hopper enemy animations (35 frames, sprite base 800, seg3:0x4885).</summary>
    public static readonly CharacterAnimSet Hopper = new("Hopper", 0x0D,
    [
        new("hop", [7, 8, 9, 0, 0, 1, 1, 2, 3], AnimMode.Loop, "state 1; script DS:0xCD0; main hop cycle"),
        new("leap", [4, 4, 4, 5, 5, 6], AnimMode.Once, "state 2; rising half of the jump -> fall"),
        new("fall", [7], AnimMode.Pose, "state 3; airborne until landing"),
        new("turn", Rng(10, 14), AnimMode.Once, "state 4; flips facing at end"),
        new("inflate", Rng(15, 23), AnimMode.Once, "state 6; pumped by shots, +2 frames per hit"),
        new("deflate", Rev(Rng(15, 23)), AnimMode.Once, "state 6; counter decays 1/tick when the shots stop"),
        new("fall_away", [24], AnimMode.Pose, "state 7; knocked away"),
        new("walk", Rng(25, 34), AnimMode.Loop, "state 5; 10-frame walk, 2px/tick"),
    ], RosterOrdinal: 7);
}
