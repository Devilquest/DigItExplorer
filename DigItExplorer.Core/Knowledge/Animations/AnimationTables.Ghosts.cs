namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Shared animation sequence for all Spookstone mimic ghost variants (seg3:0x3E86).</summary>
    private static readonly IReadOnlyList<AnimationDef> GhostAnims =
    [
        new("idle", Rev(Rng(0, 9)), AnimMode.Loop, "state 0; seg3:0x3F5D; counter counts down"),
        new("turn", Rng(10, 14), AnimMode.Once, "state 1; flips facing at end"),
        new("vanish", Rng(15, 21), AnimMode.Once, "state 2; entered only from the shot handler"),
        new("hidden", [21], AnimMode.Pose, "state 4; held, jitters, then returns"),
        new("reappear", Rev(Rng(15, 21)), AnimMode.Once, "state 3; seg3:0x401B; vanish backward"),
    ];

    /// <summary>Calculates frame crop rectangles for Ghost Slugger on page 0.</summary>
    private static Dictionary<int, FrameRect> GhostSluggerRects()
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < 22; i++) rects[i] = new FrameRect(0, (i % 9) * 33, (i / 9) * 20, 32, 19);
        return rects;
    }

    /// <summary>Calculates frame crop rectangles for Ghost Draggo on page 0.</summary>
    private static Dictionary<int, FrameRect> GhostDraggoRects()
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < 22; i++) rects[i] = new FrameRect(0, (i & 7) * 39, (i >> 3) * 31 + 60, 38, 30);
        return rects;
    }

    /// <summary>Calculates multi-page frame crop rectangles for Ghost Rocker.</summary>
    private static Dictionary<int, FrameRect> GhostRockerRects()
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < 10; i++) rects[i] = new FrameRect(0, 30 * i, 153, 29, 25);
        for (int i = 10; i < 20; i++) rects[i] = new FrameRect(1, 30 * (i - 10), 0, 29, 25);
        for (int i = 20; i < 22; i++) rects[i] = new FrameRect(1, 30 * (i - 20), 26, 29, 25);
        return rects;
    }

    /// <summary>Calculates frame crop rectangles for Ghost Pyrosaur on page 1.</summary>
    private static Dictionary<int, FrameRect> GhostPyrosaurRects()
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < 22; i++) rects[i] = new FrameRect(1, (i % 7) * 43, (i / 7) * 37 + 52, 42, 36);
        return rects;
    }

    /// <summary>Ghost Slugger mimic enemy (WO_GHOST, variant 0, category 0x13).</summary>
    public static readonly CharacterAnimSet GhostSlugger = new("Ghost Slugger", 0x13, GhostAnims,
        GhostSluggerRects(), FixedSheet: "WO_GHOST", FixedPalette: "LVL040.PAL", FixedLocation: "Spookstone", RosterOrdinal: 12);

    /// <summary>Ghost Draggo mimic enemy (WO_GHOST, variant 1, category 0x13).</summary>
    public static readonly CharacterAnimSet GhostDraggo = new("Ghost Draggo", 0x13, GhostAnims,
        GhostDraggoRects(), FixedSheet: "WO_GHOST", FixedPalette: "LVL040.PAL", FixedLocation: "Spookstone", RosterOrdinal: 14);

    /// <summary>Ghost Rocker mimic enemy (WO_GHOST, variant 2, category 0x13).</summary>
    public static readonly CharacterAnimSet GhostRocker = new("Ghost Rocker", 0x13, GhostAnims,
        GhostRockerRects(), FixedSheet: "WO_GHOST", FixedPalette: "LVL040.PAL", FixedLocation: "Spookstone", RosterOrdinal: 13);

    /// <summary>Ghost Pyrosaur mimic enemy (WO_GHOST, variant 3, category 0x13).</summary>
    public static readonly CharacterAnimSet GhostPyrosaur = new("Ghost Pyrosaur", 0x13, GhostAnims,
        GhostPyrosaurRects(), FixedSheet: "WO_GHOST", FixedPalette: "LVL040.PAL", FixedLocation: "Spookstone", RosterOrdinal: 15);
}
