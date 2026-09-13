using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Core.Maps;

/// <summary>Composes the water world exit sign graphic as an isolated still.</summary>
public static class ExitSignCompositor
{
    /// <summary>Resource filename containing the exit sign sprite.</summary>
    public const string Sheet = "GENERAL.SPF";

    /// <summary>Palette resource filename for exit sign rendering.</summary>
    public static string Palette => SkinCatalog.SuffixPal[""];

    // Matches the water exit sign crop in EntityArtStamper.
    private static readonly (int X1, int Y1, int X2, int Y2) Rect = (0, 123, 27, 149);

    /// <summary>Composes the exit sign sprite still.</summary>
    public static SpriteRenderResult? Compose(Func<string, byte[]?> loadResource)
    {
        var sheetBytes = loadResource(Sheet);
        if (sheetBytes is null) return null;

        var palBytes = loadResource(Palette);
        if (palBytes is null || palBytes.Length < 768) return null;
        var palette = VgaPalette.From6Bit(palBytes.AsSpan(0, 768));

        var frame = SheetImage.Read(sheetBytes).Frames[0];
        var (x1, y1, x2, y2) = Rect;
        var cell = SpriteSheetSlicer.RectCell(frame, x1, y1, x2, y2);

        var canvas = new SpriteCanvas(cell.W, cell.H);
        canvas.DrawIndexed(cell, palette);
        return new SpriteRenderResult(canvas.Rgb, canvas.Alpha, cell.W, cell.H, ShowedGraphic: true,
            ShowedCollision: false);
    }
}
