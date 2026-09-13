namespace DigItExplorer.Core.Minigames;

/// <summary>Identifies the specific bonus minigame board.</summary>
public enum MinigameBoard { FlipIt, StopIt, SpinIt, FindIt }

/// <summary>Layout coordinate constants and grid offsets for minigame boards.</summary>
internal static class MinigameBoardPositions
{
    public const int Width = 320, Height = 200;

    /// <summary>Vertical anchor coordinates for attempts counter icons.</summary>
    public static readonly int[] AttemptsIconY = [24, 48, 72];
    public const int AttemptsIconX = 8;

    /// <summary>Gold pickup frame indices used as attempts icons per minigame.</summary>
    public static readonly IReadOnlyDictionary<MinigameBoard, int> AttemptsIconFrame = new Dictionary<MinigameBoard, int>
    {
        [MinigameBoard.FlipIt] = 1, // Shovel (caves)
        [MinigameBoard.StopIt] = 3, // Diving Helmet (water)
        [MinigameBoard.SpinIt] = 2, // Pickaxe (snow)
        [MinigameBoard.FindIt] = 5, // Torch (underworld)
    };

    /// <summary>Anchor coordinates for the Spin It! pointer sprite.</summary>
    public const int SpinItPointerX = 127, SpinItPointerY = 66;

    /// <summary>Grid anchor coordinates for Flip It! card columns and rows.</summary>
    public static readonly int[] FlipItCardX = [37, 99, 161, 223];
    public static readonly int[] FlipItCardY = [7, 69, 131];

    /// <summary>Anchor coordinates for Stop It! and Find It! panel pieces.</summary>
    public static readonly int[] PanelX = [60, 139, 218];
    public const int PanelY = 70;
    public const int PanelCellW = 42, PanelCellH = 37;

    /// <summary>Vertical baseline coordinate for panel prize labels.</summary>
    public const int PanelLabelY = 112;

    /// <summary>Bounding rectangle for Draggo and Drakko panel piece cells.</summary>
    public static readonly (int X1, int Y1, int X2, int Y2) PanelPieceCell = (1, 1, 42, 37);
}
