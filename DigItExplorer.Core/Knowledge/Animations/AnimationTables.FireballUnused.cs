namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Unused fireball sprite copies embedded on page 1 of Pyrosaur sheets.</summary>
    public static readonly CharacterAnimSet FireballUnused = new("Fireball (unused)", 0x09,
    [
        new("flight_intro", Rng(0, 8), AnimMode.Once, "unused; state 0 flight startup"),
        new("flight_loop", Rng(9, 12), AnimMode.Loop, "unused; state 0 flight loop"),
        new("impact", Rng(13, 19), AnimMode.Once, "unused; state 1 impact explosion"),
    ],
        Rects: BuildFireballUnusedRects());

    /// <summary>Calculates frame crop rectangles for unused fireball sprites on page 1.</summary>
    private static Dictionary<int, FrameRect> BuildFireballUnusedRects()
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < 20; i++)
            rects[i] = new FrameRect(1, i * 16, 164, 15, 13);
        return rects;
    }
}
