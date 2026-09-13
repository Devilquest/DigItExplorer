namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Fireball projectile animations (WO_FBALL, sprite base 0x230, seg3:0x2DEE).</summary>
    public static readonly CharacterAnimSet Fireball = new("Fireball", null,
    [
        new("flight_intro", Rng(0, 8), AnimMode.Once, "state 0; flight startup"),
        new("flight_loop", Rng(9, 12), AnimMode.Loop, "state 0; flight loop"),
        new("impact", Rng(13, 19), AnimMode.Once, "state 1; impact explosion"),
    ],
        Rects: BuildFireballRects(),
        FixedSheet: "WO_FBALL",
        // Pinned for consistency with the other effect sheets rather than to change any pixel: the
        // unsuffixed default resolves these particular indices to the same RGB anyway.
        FixedPalette: "LVL000.PAL",
        FixedLocation: "Pyrosaur, Grock & Supreme Spurkasaur");

    /// <summary>Calculates frame crop rectangles for the fireball projectile sheet.</summary>
    private static Dictionary<int, FrameRect> BuildFireballRects()
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < 20; i++)
            rects[i] = new FrameRect(0, i * 16, 164, 15, 13);
        return rects;
    }
}
