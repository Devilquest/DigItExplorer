namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Rocker enemy animations (39 frames, sprite base 400, seg3:0x290D).</summary>
    public static readonly CharacterAnimSet Rocker = new("Rocker", 0x08,
    [
        new("sniff", [0, 1, 2, 3, 4, 5, 4, 5, 4, 5, 4, 3, 2, 1, 0], AnimMode.Once,
            "state 0; script DS:0xB5C", Seamless: true),
        new("roll", [6, 7, 8, 9, .. Rng(10, 13), .. Rng(10, 13), .. Rng(10, 13), .. Rng(10, 13), .. Rng(10, 13), 10, 9, 8, 7, 6],
            AnimMode.Once, "state 3; script DS:0xB84; the rolling attack", Seamless: true),
        new("fall", Rng(10, 13), AnimMode.Loop, "state 4; airborne ball; lands into roll uncurl or despawns"),
        new("turn", Rng(14, 18), AnimMode.Once, "state 2; script DS:0xB7A; flips facing at end"),
        new("inflate", Rng(19, 27), AnimMode.Once, "state 5; pumped by shots, +2 frames per hit"),
        new("deflate", Rev(Rng(19, 27)), AnimMode.Once, "state 5; counter decays 1/tick when the shots stop"),
        new("fall_away", [28], AnimMode.Pose, "state 6; knocked away"),
        new("walk", Rng(29, 38), AnimMode.Loop, "state 1; 10-frame walk, 2px/tick, wall -> turn"),
    ], RosterOrdinal: 2);
}
