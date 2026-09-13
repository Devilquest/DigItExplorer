namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Common swimming and turning animations shared by all aquatic enemy species (seg3:0x4133).</summary>
    private static readonly IReadOnlyList<AnimationDef> FishAnims =
    [
        new("swim", Rng(0, 9), AnimMode.Loop, "state 0; seg3:0x416E; 10-frame swim cycle"),
        new("turn", Rng(10, 14), AnimMode.Once, "state 1; seg3:0x41EB; flips facing at end"),
    ];

    /// <summary>Calculates frame crop rectangles for a row-major grid block on a sheet page.</summary>
    private static Dictionary<int, FrameRect> Grid(int page, int x0, int y0, int cols, int sx, int sy, int w, int h, int n)
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < n; i++) rects[i] = new FrameRect(page, x0 + (i % cols) * sx, y0 + (i / cols) * sy, w, h);
        return rects;
    }

    /// <summary>Aqua Slugger aquatic reskin (fixed sheet WO_FISH, category 0x12).</summary>
    public static readonly CharacterAnimSet AquaSlugger = new("Aqua Slugger", 0x12, FishAnims,
        Grid(0, 0, 142, 9, 33, 19, 32, 18, 15), FixedSheet: "WO_FISH", FixedPalette: "LVL200.PAL", FixedLocationWorld: World.Water, RosterOrdinal: 20);

    /// <summary>Sea Draggo aquatic reskin (fixed sheet WO_FISH, category 0x12).</summary>
    public static readonly CharacterAnimSet SeaDraggo = new("Sea Draggo", 0x12, FishAnims,
        Grid(0, 0, 42, 8, 40, 35, 39, 34, 15), FixedSheet: "WO_FISH", FixedPalette: "LVL200.PAL", FixedLocationWorld: World.Water, RosterOrdinal: 21);

    /// <summary>Rockerfish aquatic reskin (fixed sheet WO_FISH, category 0x12).</summary>
    public static readonly CharacterAnimSet Rockerfish = new("Rockerfish", 0x12, FishAnims,
        Grid(0, 0, 0, 9, 35, 21, 34, 20, 15), FixedSheet: "WO_FISH", FixedPalette: "LVL200.PAL", FixedLocationWorld: World.Water, RosterOrdinal: 22);

    /// <summary>Sea Spurk aquatic reskin (fixed sheet WO_FISH, category 0x12).</summary>
    public static readonly CharacterAnimSet SeaSpurk = new("Sea Spurk", 0x12, FishAnims,
        Grid(1, 0, 24, 9, 35, 21, 34, 20, 15), FixedSheet: "WO_FISH", FixedPalette: "LVL200.PAL", FixedLocationWorld: World.Water, RosterOrdinal: 26);

    /// <summary>Hopperfish aquatic reskin (fixed sheet WO_FISH, category 0x12).</summary>
    public static readonly CharacterAnimSet Hopperfish = new("Hopperfish", 0x12, FishAnims,
        Grid(0, 0, 112, 10, 32, 15, 31, 14, 15), FixedSheet: "WO_FISH", FixedPalette: "LVL200.PAL", FixedLocationWorld: World.Water, RosterOrdinal: 27);

    /// <summary>Nirpies aquatic reskin (fixed sheet WO_FISH, category 0x12).</summary>
    public static readonly CharacterAnimSet Nirpies = new("Nirpies", 0x12, FishAnims,
        Grid(1, 0, 0, 13, 24, 12, 23, 11, 15), FixedSheet: "WO_FISH", FixedPalette: "LVL200.PAL", FixedLocationWorld: World.Water, RosterOrdinal: 28);
}
