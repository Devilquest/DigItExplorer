namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Nirp enemy animations (38 frames, sprite base 600, seg3:0x3833).</summary>
    public static readonly CharacterAnimSet Nirp = new("Nirp", 0x0A,
    [
        new("fly_egg", Rng(0, 8), AnimMode.Loop, "state 0 while carrying the egg"),
        new("turn_egg", Rng(9, 13), AnimMode.Once, "state 1 while carrying"),
        new("fly", Rng(14, 22), AnimMode.Loop, "state 0 after dropping the egg"),
        new("turn", Rng(23, 27), AnimMode.Once, "state 1 after dropping the egg"),
        new("inflate", Rng(28, 36), AnimMode.Once, "state 2; pumped by shots, +2 frames per hit"),
        new("deflate", Rev(Rng(28, 36)), AnimMode.Once, "state 2; counter decays 1/tick when the shots stop"),
        new("fall_away", [37], AnimMode.Pose, "state 3; knocked away"),
    ], RosterOrdinal: 8);

    /// <summary>Calculates frame crop rectangles for Nirpling sprites on fixed rows.</summary>
    private static Dictionary<int, FrameRect> NirplingRects()
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < 14; i++) rects[i] = new FrameRect(0, 21 * i, 150, 20, 17);
        for (int i = 22; i < 35; i++) rects[i] = new FrameRect(0, 21 * (i - 22), 181, 20, 17);
        return rects;
    }

    /// <summary>Nirpling enemy animations (sprite base 660, seg3:0x335D).</summary>
    public static readonly CharacterAnimSet Nirpling = new("Nirpling", 0x0B,
    [
        new("run", [0, 1, 2, 3], AnimMode.Loop, "state 0 while counter > 6"),
        new("hop_rise", [4, 5, 6, 7], AnimMode.Loop, "state 1 while counter < 7"),
        new("fall_away", [5], AnimMode.Pose, "state 5; knocked away"),
        new("fall", [8, 9, 10, 11], AnimMode.Loop, "state 2; DS:0xBFA is its Y-speed arc"),
        new("inflate", [12], AnimMode.Pose, "state 4; the inflate idiom, 1-frame"),
        new("run_windup", [22, 23, 24, 25], AnimMode.Loop, "state 0, last 6 ticks before the hop"),
        new("hop_apex", [26, 27], AnimMode.Loop, "state 1, counter 7-8"),
        new("fall_start", [28, 29, 30], AnimMode.Once, "state 2, first 3 ticks"),
        new("turn", Rng(30, 34), AnimMode.Once, "state 3; flips facing at end"),
    ], NirplingRects());
}
