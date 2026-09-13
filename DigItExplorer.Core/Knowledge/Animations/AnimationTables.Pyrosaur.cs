namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Pyrosaur enemy animations (46 frames, sprite base 0x1F4, seg3:0x2FC3).</summary>
    public static readonly CharacterAnimSet Pyrosaur = new("Pyrosaur", 0x09,
    [
        new("walk", Rng(0, 15), AnimMode.Loop, "state 0; seg3:0x303C; X ±2, fire timer -> telegraph"),
        new("turn", Rng(16, 20), AnimMode.Once, "state 1; seg3:0x309B; flips facing at end"),
        new("inflate", Rng(21, 31), AnimMode.Once, "state 4; pumped by shots"),
        new("deflate", Rev(Rng(21, 31)), AnimMode.Once, "state 4; inflate reversed when not fully pumped"),
        new("telegraph", [34, 35, 34, 35, 34, 35, 34, 35, 34, 35], AnimMode.Once,
            "state 3; seg3:0x3132; wind-up flicker before fire"),
        new("fire_breath", Rng(36, 42), AnimMode.Once, "state 2; seg3:0x30B5; spits a ball at frame 39"),
        // 45, not 43 or 44: those two are among the empty cells named in the summary.
        new("fall_away", [45], AnimMode.Pose, "state 6; knocked away"),
    ], RosterOrdinal: 38);
}
