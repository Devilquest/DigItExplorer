namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Player-side effect animations on GENERAL.SPF (bullets, smoke, hit puffs, dust).</summary>
    public static readonly CharacterAnimSet GeneralEffects = new("General Effects", null,
    [
        new("bullets", Rng(0, 3), AnimMode.Loop, "base 2; the player's own projectiles"),
        new("jetpack_smoke", Rng(4, 7), AnimMode.Loop, "base 90; exhaust, one puff every 3 ticks",
            DisplayName: "Jet Pack smoke"),
        new("wall_hit_earth", Rng(8, 13), AnimMode.Once, "base 6; projectile hitting earth"),
        new("wall_hit_gray", Rng(14, 19), AnimMode.Once, "base 6; projectile hitting gray stone"),
        new("jump_dirt", Rng(20, 39), AnimMode.Once, "base 220; dust thrown up on landing"),
    ],
        Rects: BuildGeneralEffectsRects(),
        FixedSheet: "GENERAL",
        FixedPalette: "LVL000.PAL",
        FixedLocation: "Dug (player)");

    /// <summary>Calculates frame crop rectangles for general player effect sprites.</summary>
    private static Dictionary<int, FrameRect> BuildGeneralEffectsRects()
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < 4; i++)
            rects[i] = new FrameRect(0, 31 + i * 10, 0, 10, 5);
        for (int i = 0; i < 4; i++)
            rects[4 + i] = new FrameRect(0, 72 + i * 6, 0, 5, 5);
        for (int i = 0; i < 6; i++)
        {
            rects[8 + i] = new FrameRect(0, 102 + i * 15, 0, 14, 11);
            rects[14 + i] = new FrameRect(0, 102 + i * 15, 12, 14, 11);
        }
        for (int i = 0; i < 20; i++)
            rects[20 + i] = new FrameRect(0, i * 16, 191, 16, 8);
        return rects;
    }
}
