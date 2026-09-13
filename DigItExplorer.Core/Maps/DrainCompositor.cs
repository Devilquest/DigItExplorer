using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Core.Maps;

/// <summary>Visual and collider state of a water world drain.</summary>
public enum DrainState
{
    /// <summary>Open active drain entrance.</summary>
    Open,

    /// <summary>Sealed drain entrance after bonus completion.</summary>
    Sealed,
}

/// <summary>Composes water world drain graphic and collider templates as stills.</summary>
public static class DrainCompositor
{
    /// <summary>Resource filename for drain sprites and collider templates.</summary>
    public const string Sheet = "WO_DRAIN.SPF";

    /// <summary>Palette resource filename for water world drain rendering.</summary>
    public static string Palette => SkinCatalog.SuffixPal[""];

    // Cell bounds on WO_DRAIN: columns are states (open/sealed), rows are planes (collider/graphic).
    private static (int X1, int Y1, int X2, int Y2) GraphicRect(DrainState state)
        => state == DrainState.Open ? (1, 52, 89, 101) : (91, 52, 179, 101);

    private static (int X1, int Y1, int X2, int Y2) ColliderRect(DrainState state)
        => state == DrainState.Open ? (1, 1, 89, 50) : (91, 1, 179, 50);

    /// <summary>Composes a drain state sprite and collider still according to render options.</summary>
    public static SpriteRenderResult? Compose(Func<string, byte[]?> loadResource, DrainState state,
        SpriteRenderOptions options)
    {
        if (!options.ShowGraphic && !options.ShowCollision) return null;

        var sheetBytes = loadResource(Sheet);
        if (sheetBytes is null) return null;

        var palBytes = loadResource(Palette);
        if (palBytes is null || palBytes.Length < 768) return null;
        var palette = VgaPalette.From6Bit(palBytes.AsSpan(0, 768));

        var frame = SheetImage.Read(sheetBytes).Frames[0];
        var (gx1, gy1, gx2, gy2) = GraphicRect(state);
        var (cx1, cy1, cx2, cy2) = ColliderRect(state);
        var graphic = SpriteSheetSlicer.RectCell(frame, gx1, gy1, gx2, gy2);
        var collider = SpriteSheetSlicer.RectCell(frame, cx1, cy1, cx2, cy2);
        int w = graphic.W, h = graphic.H;

        var canvas = new SpriteCanvas(w, h);
        if (options.ShowGraphic)
            canvas.DrawIndexed(graphic, palette);
        if (options.ShowCollision)
            DrawCollider(canvas, collider);

        return new SpriteRenderResult(canvas.Rgb, canvas.Alpha, w, h, options.ShowGraphic, options.ShowCollision);
    }

    /// <summary>Blends walkable collider code 64 onto the canvas.</summary>
    private static void DrawCollider(SpriteCanvas canvas, SpriteCell cell)
    {
        var (cr, cg, cb, ca) = MaterialPalette.OverlayColorOf(64);
        for (int i = 0; i < cell.Pixels.Length; i++)
        {
            if (cell.Pixels[i] != 64) continue;
            canvas.Blend(i, cr, cg, cb, ca);
        }
    }
}
