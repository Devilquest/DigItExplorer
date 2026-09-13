namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Papa Spurk enemy animations (36 frames, sprite base 0x1C2, seg3:0x45B9).</summary>
    public static readonly CharacterAnimSet PapaSpurk = new("Papa Spurk", 0x0E,
    [
        new("walk", Rng(0, 9), AnimMode.Loop, "state 0; X ±2 per tick"),
        new("turn", Rng(10, 14), AnimMode.Once, "states 1 & 2; flips facing at end"),
        new("slam", [.. Rng(15, 23), .. Rng(15, 23), .. Rng(15, 23), 15, 15, 15, 15, 15],
            AnimMode.Once, "state 3; script DS:0xC90, after charge hits a wall", Seamless: true),
        new("charge", Rng(24, 33), AnimMode.Loop, "state 4; X ±4 charge (shot-enraged, double speed)"),
        // 35, not 34: 34 is the empty cell named in the summary.
        new("fall_away", [35], AnimMode.Pose, "state 5; killed, flies off"),
    ], RosterOrdinal: 33);
}
