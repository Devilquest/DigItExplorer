namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>The player's on-screen name, shared by all player sets.</summary>
    private const string PlayerName = "Dug";

    /// <summary>Location label for power-up sets loaded in all worlds.</summary>
    private const string SpecialAbilities = "Special Abilities";

    /// <summary>Calculates frame rectangles for the uniform grid shared by player sheets A, B, and D.</summary>
    private static Dictionary<int, FrameRect> DugAbGrid(int baseId, int n)
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < n; i++) rects[baseId + i] = new FrameRect(0, (i % 10) * 32, (i / 10) * 37 + 1, 32, 36);
        return rects;
    }

    /// <summary>Calculates frame rectangles for the player C sheet grid.</summary>
    private static Dictionary<int, FrameRect> DugCGrid(int n)
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < n; i++) rects[100 + i] = new FrameRect(0, (i % 9) * 33, (i / 9) * 44 + 1, 33, 43);
        return rects;
    }

    /// <summary>Calculates frame rectangles for a custom row-major grid on page 0.</summary>
    private static Dictionary<int, FrameRect> DugGrid(int baseId, int cols, int strideX, int strideY, int w, int h,
        int n, int xOffset = 0, int yOffset = 0)
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < n; i++)
            rects[baseId + i] = new FrameRect(0, xOffset + (i % cols) * strideX, yOffset + (i / cols) * strideY, w, h);
        return rects;
    }

    /// <summary>Player level actions (sheet A, ids 0-49, seg3:0x9C2D).</summary>
    public static readonly CharacterAnimSet Dug = new(PlayerName, null,
    [
        new("skid", [0, 1, 0, 1, 0, 1, 2, 3], AnimMode.Once, "state 7; script DS:0xE4A, brake after a long run"),
        new("run_start", Rng(4, 9), AnimMode.Once, "state 1 entry; dx DS:0xE5A; frame 3 never displayed"),
        new("run", Rng(10, 19), AnimMode.Loop, "state 1; 6 px/tick"),
        new("jump_takeoff", [20, 20, 21, 22], AnimMode.Once, "script DS:0x5E"),
        new("jump_rise", Rng(23, 28), AnimMode.Once, "state 3; Y arc DS:0xE2A"),
        new("fall", [29], AnimMode.Pose, "state 4"),
        new("land", [30, 31, 31], AnimMode.Once, "script DS:0x1E; + 3 dust puffs"),
        new("jump_full", [20, 20, 21, 22, .. Rng(23, 28), 29, 29, 29, 29, 29, 29, 30, 31, 31],
            AnimMode.Once, "takeoff + rise + fall + land chained"),
        new("idle", [32, 33], AnimMode.Loop, "state 0; gun holster cord shimmer"),
        new("idle_turn", [34, 35, 34, 35, 36, 37, 38], AnimMode.Once, "state 0; facing turn in place, 39 skipped"),
        new("push_wall", [34, 35], AnimMode.Loop, "state 1 blocked by a wall"),
        new("rope_climb", Rng(44, 49), AnimMode.Loop, "state 5; rope material 0x50"),
    ], Rects: DugAbGrid(0, 50), SheetLetter: 'A');

    /// <summary>Player crouching and crawling actions (sheet B, ids 50-75, seg3:0x9CB9).</summary>
    public static readonly CharacterAnimSet DugCrouch = new("DugCrouch", null,
    [
        new("crouch_down", Rng(50, 54), AnimMode.Once, "state 2 entry"),
        new("crawl", Rng(54, 63), AnimMode.Loop, "state 2 while moving"),
        new("crawl_blocked", [53, 54], AnimMode.Loop, "state 2 blocked"),
        new("crouch_up", Rev(Rng(50, 54)), AnimMode.Once, "state 2 exit (reversed)"),
    ], Rects: DugAbGrid(50, 26), SheetLetter: 'B', DisplayName: PlayerName);

    /// <summary>Player idle wait sequences on sheet B.</summary>
    public static readonly CharacterAnimSet DugWait = new("DugWait", null,
    [
        new("idle_wait_frames",
            [.. new[] { 64, 66, 68, 70, 72, 74 }.SelectMany(p => new[] { p, p ^ 1, p, p ^ 1, p, p ^ 1, p, p ^ 1 })],
            AnimMode.Loop, "the six pairs in sheet order (raw material, looking up/down in-game)"),
        new("idle_wait_sim",
            [.. new[] { 64, 66, 68, 70, 72, 74 }.SelectMany(p => new[] { p, p ^ 1, p, p ^ 1, p, p ^ 1, p, p ^ 1 })],
            AnimMode.Loop, "placeholder: regenerated live via DugWaitSimulator, see the class summary above"),
    ], Rects: DugAbGrid(50, 26), SheetLetter: 'B', DisplayName: PlayerName);

    /// <summary>Player digging actions (sheet C, ids 100-128, seg3:0x9D44).</summary>
    public static readonly CharacterAnimSet DugDig = new("DugDig", null,
    [
        new("dig_windup", Rng(100, 104), AnimMode.Once, "state 6 entry"),
        new("dig_drill", Rng(105, 108), AnimMode.Loop, "state 6 with a hole; +DugDirt overlay, body sinks"),
        // The three drill cycles at the front are in the game's own script, not padding added here.
        new("dig_fail", [105, 106, 107, 108, 105, 106, 107, 108, 105, 106, 107, 108,
                          .. Rng(109, 123), .. Rng(116, 123), .. Rng(124, 127)],
            AnimMode.Once, "state 6 without a hole; script DS:0x178, drill x3 + head-shake"),
    ], Rects: DugCGrid(29), SheetLetter: 'C', DisplayName: PlayerName);

    /// <summary>Super Dug power-up flying animation (DUG1A, ids 200-207, seg3:0x739F).</summary>
    public static readonly CharacterAnimSet DugSuper = new("DugSuper", null,
    [
        new("super_dug_fly", Rng(200, 207), AnimMode.Loop, "state 0xA; masked &7 loop",
            DisplayName: "Super Dug fly"),
    ], DugGrid(200, 4, 64, 32, 46, 23, 8), FixedSheet: "DUG1A", FixedPalette: "LVL000.PAL",
        FixedLocation: SpecialAbilities, DisplayName: PlayerName);

    /// <summary>Jetpack power-up animations (DUG2A, ids 210-216, seg3:0x725D).</summary>
    public static readonly CharacterAnimSet DugJetpack = new("DugJetpack", null,
    [
        new("jetpack_hover", [211, 210], AnimMode.Loop, "state 0xB at rest; 211 with -tick&1 jitter",
            DisplayName: "Jet Pack hover"),
        new("jetpack_tilt", Rng(210, 216), AnimMode.Once, "pose sampler: frame = 210 + |Xspeed|/2 + 1",
            DisplayName: "Jet Pack tilt"),
    ], DugGrid(210, 7, 32, 0, 32, 36, 7, yOffset: 1), FixedSheet: "DUG2A", FixedPalette: "LVL000.PAL",
        FixedLocation: SpecialAbilities, DisplayName: PlayerName);

    /// <summary>Dig hole overlay animation (DUGDIRT, sprite base 970).</summary>
    public static readonly CharacterAnimSet DugDirt = new("DugDirt", null,
    [
        new("dig_hole", Rng(0, 26), AnimMode.Once, "overlay under the drill"),
    ], DugGrid(0, 6, 53, 31, 53, 31, 27), FixedSheet: "DUGDIRT", FixedPalette: "LVL000.PAL",
        FixedLocation: "Dug (player)", DisplayName: "Dug Dirt");

    /// <summary>Player swimming animations in water levels (DUG7A, seg3:0x7599).</summary>
    public static readonly CharacterAnimSet DugSwim = new("DugSwim", null,
    [
        new("swim", Rng(0, 9), AnimMode.Loop, "state 0xC; 10-frame stroke"),
        new("swim_left_map", Rng(12, 21), AnimMode.Loop, "water world map only; mirror of 0-9 (map ids 10-19)"),
        new("swim_turn", [24, 25, 26], AnimMode.Once, "state 0xC turn; 23 skipped"),
    ], DugGrid(0, 6, 48, 35, 47, 34, 30), FixedSheet: "DUG7A", FixedPalette: "LVL200.PAL",
        DisplayName: PlayerName, FixedLocationWorld: World.Water);

    /// <summary>Player walking and standing animations on the world map (sheet D, seg2:0x1151).</summary>
    public static readonly CharacterAnimSet DugMap = new("DugMap", null,
    [
        new("map_walk_right", Rng(0, 9), AnimMode.Loop, "map direction 3"),
        new("map_walk_left", Rng(10, 19), AnimMode.Loop, "map direction 2"),
        new("map_walk_down", Rng(20, 27), AnimMode.Loop, "map direction 1 (toward camera)"),
        new("map_walk_up", Rng(30, 37), AnimMode.Loop, "map direction 0 (away)"),
        new("map_stand_right", [40], AnimMode.Pose, "arrived walking right"),
        new("map_stand_left", [41], AnimMode.Pose, "arrived walking left"),
        new("map_stand_front", [42], AnimMode.Pose, "arrived walking down; also the map-entry idle (seg2:0x914)"),
        new("map_stand_back", [43], AnimMode.Pose, "arrived walking up"),
    ], Rects: DugAbGrid(0, 44), SheetLetter: 'D', DisplayName: PlayerName);
}
