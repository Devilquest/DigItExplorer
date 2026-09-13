namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Spurk enemy animations (30 frames, sprite base 0xFA, seg3:0x431D).</summary>
    public static readonly CharacterAnimSet Spurk = new("Spurk", 0x0C,
    [
        new("retract", Rng(0, 6), AnimMode.Once, "state 2; pulls into shell when hit"),
        new("hidden", [6], AnimMode.Pose, "state 3 (timer phase); 35 ticks, jitters"),
        new("emerge", Rng(6, 13), AnimMode.Once, "state 3 (post-timer) -> walk/turn"),
        new("walk", Rng(14, 23), AnimMode.Loop, "state 0; gait mask DS:0xC86"),
        new("turn", Rng(24, 28), AnimMode.Once, "state 1; flips facing at end"),
        new("fall_away", [29], AnimMode.Pose, "state 4; killed, flies off"),
    ], RosterOrdinal: 6);
}
