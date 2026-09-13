namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>In-game HUD element animations and meters on GENERAL.SPF (seg3:0x79F3).</summary>
    public static readonly CharacterAnimSet HudGeneral = new("HUD (General)", null,
    [
        new("life_icon_0", [0], AnimMode.Pose, "sprite 0, x=0", DisplayName: "Life icon",
            VariantWorld: World.Caves),
        new("life_icon_1", [1], AnimMode.Pose, "sprite 0, x=32", DisplayName: "Life icon",
            VariantWorld: World.Water),
        new("life_icon_2", [2], AnimMode.Pose, "sprite 0, x=64", DisplayName: "Life icon",
            VariantWorld: World.Snow),
        new("life_icon_3", [3], AnimMode.Pose, "sprite 0, x=96; no world registers this column",
            DisplayName: "Life icon (unused)"),
        new("life_icon_4", [4], AnimMode.Pose, "sprite 0, x=128", DisplayName: "Life icon",
            VariantWorld: World.Underworld),
        new("gems", [5], AnimMode.Pose, "sprite 1; drawn beside the gem counter",
            DisplayName: GemIconName),
        new("energy", Rev(Rng(6, 10)), AnimMode.Once, "sprites 49..45; DS:0x1298 counts 4 down to 0",
            DisplayName: EnergyMeterName, StepsAreState: true),
        new("time", Rev(Rng(11, 42)), AnimMode.Once, "sprites 44..13 over sprite 12; DS:0x129A >> 7",
            DisplayName: TimeMeterName, Underlay: (43, 5, 2), StepsAreState: true),
    ],
        Rects: BuildHudGeneralRects(),
        FixedSheet: "GENERAL",
        FixedPalette: "LVL000.PAL",
        FixedLocation: "All levels",
        TransparentIndices: new HashSet<byte> { 1 },
        DisplayName: "In-game HUD");

    // Read from the game rather than written as literals: these three are named by the pickups that refill
    // them. The gem one is pluralized because it labels a running count, not one collectible.
    private static string GemIconName => EntityCategories.LabelOf(0x02) + "s";
    private static string EnergyMeterName => EntityCategories.SubtypeNameOf(0x01, 0)!;
    private static string TimeMeterName => EntityCategories.SubtypeNameOf(0x01, 4)!;

    /// <summary>Calculates frame crop rectangles for HUD icons, meters, and frame overlays.</summary>
    private static Dictionary<int, FrameRect> BuildHudGeneralRects()
    {
        var rects = new Dictionary<int, FrameRect>();
        // seg3:0x7F89: sprite 0, (32w, 93)-(32w+30, 121).
        for (int w = 0; w < 5; w++)
            rects[w] = new FrameRect(0, 32 * w, 93, 31, 29);
        // sprite 1, seg3:0x7FA2, (43,8)-(65,27).
        rects[5] = new FrameRect(0, 43, 8, 23, 20);
        // sprites 45-49, seg3:0x7FED, (132, 43+10i)-(170, 51+10i).
        for (int i = 0; i < 5; i++)
            rects[6 + i] = new FrameRect(0, 132, 43 + 10 * i, 39, 9);
        // seg3:0x8048: sprites 13-44, (33j, 29+8k)-(33j+31, 35+8k), registered as 13 + (31 - (8j + k)).
        for (int level = 0; level < 32; level++)
        {
            int n = 31 - level;
            rects[11 + level] = new FrameRect(0, 33 * (n / 8), 29 + 8 * (n % 8), 32, 7);
        }
        // seg3:0x7FBB: sprite 12, (0,17)-(41,27).
        rects[43] = new FrameRect(0, 0, 17, 42, 11);
        return rects;
    }
}
