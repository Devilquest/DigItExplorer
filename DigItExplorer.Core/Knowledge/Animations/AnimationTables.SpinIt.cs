namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Spin It! bonus minigame prize-wheel pointer revolution animations (GM2_SPIN).</summary>
    public static readonly CharacterAnimSet SpinIt = new("Spin It!", null,
    [
        new("spin", Rng(0, 35), AnimMode.Loop, "one revolution, 10 degrees per frame"),
    ],
        Rects: BuildSpinItRects(),
        FixedSheet: "GM2_SPIN",
        FixedLocation: "Bonus minigame",
        TransparentIndices: new HashSet<byte> { 51, 52, 53, 54, 55, 56, 171 },
        UseEmbeddedPalette: true);

    /// <summary>Calculates frame crop rectangles for the spin pointer across 3 pages.</summary>
    private static Dictionary<int, FrameRect> BuildSpinItRects()
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < 15; i++)
        {
            rects[i] = new FrameRect(0, i % 5 * 64, i / 5 * 65, 64, 64);
            rects[15 + i] = new FrameRect(1, i % 5 * 64, i / 5 * 65, 64, 64);
        }
        for (int i = 0; i < 6; i++)
            rects[30 + i] = new FrameRect(2, i % 5 * 64, i / 5 * 65, 64, 64);
        return rects;
    }
}
