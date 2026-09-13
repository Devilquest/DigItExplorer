namespace DigItExplorer.Core.Knowledge.Animations;

public static partial class AnimationTables
{
    /// <summary>Stop It! bonus minigame reel symbol piece animations (GM1_PCS row 1).</summary>
    public static readonly CharacterAnimSet StopItPieces = new("Stop It! Pieces", null,
    [
        new("draggo", [0], AnimMode.Pose, "reel symbol", DisplayName: "Draggo"),
        new("extra_dug", [1], AnimMode.Pose, "reel symbol", DisplayName: "Extra Dug"),
        new("gems25", [2], AnimMode.Pose, "reel symbol", DisplayName: "25 Gems"),
        new("gems50", [3], AnimMode.Pose, "reel symbol", DisplayName: "50 Gems"),
        new("energy", [4], AnimMode.Pose, "reel symbol; the gray/stone Dug", DisplayName: "Energy"),
    ],
        Rects: PieceRow1Rects(),
        FixedSheet: "GM1_PCS",
        FixedLocation: "Stop It!",
        UseEmbeddedPalette: true,
        TransparentIndices: new HashSet<byte> { 1 },
        DisplayName: "Pieces");

    /// <summary>Find It! bonus minigame reel symbol piece animations (GM3_PCS row 1).</summary>
    public static readonly CharacterAnimSet FindItPieces = new("Find It! Pieces", null,
    [
        new("drakko", [0], AnimMode.Pose, "reel symbol", DisplayName: "Drakko"),
        new("extra_dug", [1], AnimMode.Pose, "reel symbol", DisplayName: "Extra Dug"),
        new("gems25", [2], AnimMode.Pose, "reel symbol; no board state shows it",
            DisplayName: "25 Gems (unused)"),
        new("gems50", [3], AnimMode.Pose, "reel symbol; no board state shows it",
            DisplayName: "50 Gems (unused)"),
        new("energy", [4], AnimMode.Pose, "reel symbol; no board state shows it",
            DisplayName: "Energy (unused)"),
    ],
        Rects: PieceRow1Rects(),
        FixedSheet: "GM3_PCS",
        FixedLocation: "Find It!",
        UseEmbeddedPalette: true,
        TransparentIndices: new HashSet<byte> { 1 },
        DisplayName: "Pieces");

    /// <summary>Flip It! prize-column award icon animations (GM0_TURN page 2).</summary>
    public static readonly CharacterAnimSet FlipItPrizeIcons = new("Flip It! Prize Icons", null,
    [
        new("gems25", [0], AnimMode.Pose, "award icon", DisplayName: "25 Gems"),
        new("gems50", [1], AnimMode.Pose, "award icon", DisplayName: "50 Gems"),
    ],
        Rects: FlipItPrizeIconRects(),
        FixedSheet: "GM0_TURN",
        FixedPalette: "GM0_PAL.PAL",
        FixedLocation: "Bonus minigame",
        TransparentIndices: new HashSet<byte> { 1 },
        DisplayName: "Flip It!");

    /// <summary>Stop It! and Spin It! prize-column award icon animations (GM1_PCS row 2).</summary>
    public static readonly CharacterAnimSet StopItPrizeIcons = new("Stop It! Prize Icons", null,
    [
        new("gems25", [0], AnimMode.Pose, "award icon", DisplayName: "25 Gems"),
        new("gems50", [1], AnimMode.Pose, "award icon", DisplayName: "50 Gems"),
    ],
        Rects: PieceRow2Rects(),
        FixedSheet: "GM1_PCS",
        FixedLocation: "Bonus minigame",
        UseEmbeddedPalette: true,
        TransparentIndices: new HashSet<byte> { 1 },
        DisplayName: "Stop It! / Spin It!");

    /// <summary>Find It! prize-column award icon animations (GM3_PCS row 2).</summary>
    public static readonly CharacterAnimSet FindItPrizeIcons = new("Find It! Prize Icons", null,
    [
        new("gems25", [0], AnimMode.Pose, "award icon", DisplayName: "25 Gems"),
        new("gems50", [1], AnimMode.Pose, "award icon", DisplayName: "50 Gems"),
    ],
        Rects: PieceRow2Rects(),
        FixedSheet: "GM3_PCS",
        FixedLocation: "Bonus minigame",
        UseEmbeddedPalette: true,
        TransparentIndices: new HashSet<byte> { 1 },
        DisplayName: "Find It!");

    /// <summary>Calculates frame crop rectangles for row 1 minigame board pieces.</summary>
    private static Dictionary<int, FrameRect> PieceRow1Rects()
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < 5; i++)
            rects[i] = new FrameRect(0, 1 + i * 43, 1, 42, 37);
        return rects;
    }

    /// <summary>Calculates frame crop rectangles for row 2 minigame prize icons.</summary>
    private static Dictionary<int, FrameRect> PieceRow2Rects()
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < 2; i++)
            rects[i] = new FrameRect(0, 1 + i * 30, 41, 29, 29);
        return rects;
    }

    /// <summary>Calculates frame crop rectangles for Flip It! prize icons on page 2.</summary>
    private static Dictionary<int, FrameRect> FlipItPrizeIconRects()
    {
        var rects = new Dictionary<int, FrameRect>();
        for (int i = 0; i < 2; i++)
            rects[i] = new FrameRect(2, 1 + i * 30, 1, 28, 28);
        return rects;
    }
}
