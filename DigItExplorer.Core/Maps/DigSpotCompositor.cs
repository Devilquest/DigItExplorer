using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Maps;

/// <summary>Visual state variant of a ground dig spot marker.</summary>
public enum DigSpotState
{
    /// <summary>Standard exit marker leading to the next substage.</summary>
    Exit,

    /// <summary>Underworld recolor of the standard exit marker.</summary>
    ExitUnderworld,

    /// <summary>Faint bonus warp marker leading to a bonus zone.</summary>
    Bonus,

    /// <summary>Sealed bonus entrance after zone completion.</summary>
    BonusSealed,
}

/// <summary>Composes ground dig spot state sprites as isolated stills.</summary>
public static class DigSpotCompositor
{
    /// <summary>Resource filename for dig spot sprites.</summary>
    public const string Sheet = "WO_DAREA.SPF";

    private static int CellOf(DigSpotState state) => state switch
    {
        DigSpotState.Exit => 0,
        DigSpotState.ExitUnderworld => 3,
        DigSpotState.BonusSealed => 1,
        _ => 2,
    };

    /// <summary>Composes a dig spot state sprite still.</summary>
    public static SpriteRenderResult? Compose(Func<string, byte[]?> loadResource, DigSpotState state)
    {
        var sheetBytes = loadResource(Sheet);
        if (sheetBytes is null) return null;

        var sheetImage = SheetImage.Read(sheetBytes);
        var cells = SpriteSheetSlicer.Slice(sheetImage.Frames[0], sheetImage.Frames[0][0]); // the sheet's own corner pixel is its separator
        int index = CellOf(state);
        if (index >= cells.Count) return null;

        var cell = cells[index];
        var canvas = new SpriteCanvas(cell.W, cell.H);
        canvas.DrawIndexed(cell, sheetImage.Palette);
        return new SpriteRenderResult(canvas.Rgb, canvas.Alpha, cell.W, cell.H, ShowedGraphic: true,
            ShowedCollision: false);
    }
}
