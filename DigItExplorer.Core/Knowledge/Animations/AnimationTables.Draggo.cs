namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Draggo enemy animations (39 frames, sprite base 350, seg3:0x25D4).</summary>
    public static readonly CharacterAnimSet Draggo = new("Draggo", 0x07,
    [
        new("walk", Rng(0, 9), AnimMode.Loop, "state 0"),
        new("turn", Rng(10, 14), AnimMode.Once, "state 1; flips direction at end"),
        new("inflate", Rng(15, 23), AnimMode.Once, "state 3; pumped by shots"),
        new("deflate", Rev(Rng(15, 23)), AnimMode.Once, "state 3; inflate reversed when not fully pumped"),
        new("fall_away", [24], AnimMode.Pose, "state 4; knocked away"),
        new("scratch_head", [.. Rng(25, 35), .. Rng(29, 35), .. Rng(29, 35), 36, 37, 38],
            AnimMode.Once, "state 2; script DS:0xB22; after deflating", Seamless: true),
    ], RosterOrdinal: 1);
}
