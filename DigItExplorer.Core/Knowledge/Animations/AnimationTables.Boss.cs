namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Calculates frame crop rectangles for Supreme Spurkasaur across 6 chained pages.</summary>
    private static Dictionary<int, FrameRect> BossRects()
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < 36; i++)
            rects[i] = new FrameRect(i / 6, (i % 3) * 95, (i % 6 / 3) * 72, 94, 71);
        return rects;
    }

    /// <summary>Supreme Spurkasaur boss animations (36 frames, sprite base 0x2BC, seg3:0x4D8B).</summary>
    public static readonly CharacterAnimSet Boss = new("Supreme Spurkasaur", 0x32,
    [
        new("walk", Rng(0, 9), AnimMode.Loop, "state 3; seg3:0x4EDE; X += 6"),
        new("turn", Rng(10, 14), AnimMode.Once, "state 4; seg3:0x4FA6; flips facing at end"),
        new("tantrum", [.. Rng(15, 23), .. Rng(15, 23), .. Rng(15, 23)], AnimMode.Once,
            "state 2; script DS:0xCFA (angry jumps)"),
        new("leap", [16, 17, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 21, 22, 23], AnimMode.Once,
            "state 6; script DS:0xDBC (Y-arc)"),
        new("delay", [23], AnimMode.Pose, "state 0; seg3:0x4E79; wait to drop"),
        new("fall", [23], AnimMode.Pose, "state 1; seg3:0x4E85; drop from ceiling"),
        new("spit", [24, 25, 26, 27, 28, 29, 29, 29, 28, 27, 26, 26, 26, 26, 27, 28, 29, 29, 29,
                      28, 27, 26, 26, 26, 26, 27, 28, 29, 29, 29, 28, 27, 26, 25, 24], AnimMode.Once,
            "state 5; script DS:0xD30; a fireball on every 29, nine in all"),
        new("die", Rng(30, 35), AnimMode.Once, "state 7; seg3:0x501E; holds frame 35"),
    ], BossRects(), FixedSheet: "WO_BOSS", FixedPalette: "LVL600.PAL",
        FixedLocationWorld: World.Boss, RosterOrdinal: 42);
}
