namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Slugger enemy animations (28 frames, sprite base 300, seg3:0x222D).</summary>
    public static readonly CharacterAnimSet Slugger = new("Slugger", 0x06,
    [
        new("shell_spin", Rng(0, 3), AnimMode.Loop, "states 3/4; shell sliding"),
        new("shell_rest", [4], AnimMode.Pose, "state 2 (timer phase); wobbles"),
        new("emerge", Rng(4, 11), AnimMode.Once, "state 2 (post-timer)"),
        new("walk", Rng(12, 21), AnimMode.Loop, "state 0"),
        new("turn", Rng(22, 26), AnimMode.Once, "state 1; flips direction at end"),
        new("fall_away", [27], AnimMode.Pose, "state 5; popped"),
    ], RosterOrdinal: 0);
}
