using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.Core.Maps;

/// <summary>Plane visibility options for two-plane sprite rendering, where the two are independent rather
/// than a graphic with an optional overlay: either plane alone is a meaningful preview.</summary>
public sealed record SpriteRenderOptions(bool ShowGraphic, bool ShowCollision);

/// <summary>Composed two-plane sprite still with color, alpha, and plane visibility flags.</summary>
/// <param name="Rgb">Row-major color buffer (3 bytes per pixel).</param>
/// <param name="Alpha">Row-major alpha coverage buffer (1 byte per pixel).</param>
/// <param name="Width">Width of the composed still in pixels.</param>
/// <param name="Height">Height of the composed still in pixels.</param>
/// <param name="ShowedGraphic">Flag indicating whether graphic plane was drawn.</param>
/// <param name="ShowedCollision">Flag indicating whether collision plane was drawn.</param>
public sealed record SpriteRenderResult(byte[] Rgb, byte[] Alpha, int Width, int Height, bool ShowedGraphic,
    bool ShowedCollision);

/// <summary>Composes moving platform graphic and collider templates as stills.</summary>
public static class PlatformCompositor
{
    private const byte MvlSeparator = 255; // must stay the value ColliderStamper slices on

    /// <summary>Resolves the moving platform sheet filename for a specific world.</summary>
    internal static string SheetFor(World world) => $"WO_MVL0{(int)world}.SPF";

    /// <summary>Resolves the palette filename for a specific world's platform art.</summary>
    internal static string PaletteFor(World world) => SkinCatalog.SuffixPal[$"0{(int)world}"];

    /// <summary>Gets the number of platform cells available for a world.</summary>
    /// <param name="loadResource">Resolver function for archive resource bytes.</param>
    /// <param name="world">Target game world.</param>
    /// <returns>Count of platform cells in the world's sheet, or 0 if none.</returns>
    public static int CellCount(Func<string, byte[]?> loadResource, World world)
    {
        var sheet = loadResource(SheetFor(world));
        return sheet is null ? 0 : SpriteSheetSlicer.Slice(SheetImage.Read(sheet).Frames[0], MvlSeparator).Count;
    }

    /// <summary>Composes a moving platform cell still with graphic and collider planes.</summary>
    public static SpriteRenderResult? Compose(Func<string, byte[]?> loadResource, World world, int cell,
        SpriteRenderOptions options)
    {
        if (!options.ShowGraphic && !options.ShowCollision) return null;

        var sheetBytes = loadResource(SheetFor(world));
        if (sheetBytes is null) return null;

        var palBytes = loadResource(PaletteFor(world));
        if (palBytes is null || palBytes.Length < 768) return null;
        var palette = VgaPalette.From6Bit(palBytes.AsSpan(0, 768));

        var cells = SpriteSheetSlicer.Slice(SheetImage.Read(sheetBytes).Frames[0], MvlSeparator);
        if (cell < 0 || cell >= cells.Count) return null;

        var graphic = SpriteSheetSlicer.TopHalf(cells[cell]);
        var collider = SpriteSheetSlicer.BottomHalf(cells[cell]);
        int w = graphic.W, h = graphic.H;

        var canvas = new SpriteCanvas(w, h);
        if (options.ShowGraphic)
            canvas.DrawIndexed(graphic, palette);
        if (options.ShowCollision)
            DrawCollider(canvas, collider);

        return new SpriteRenderResult(canvas.Rgb, canvas.Alpha, w, h, options.ShowGraphic, options.ShowCollision);
    }

    /// <summary>Blends platform collider material codes onto the canvas.</summary>
    private static void DrawCollider(SpriteCanvas canvas, SpriteCell cell)
    {
        for (int i = 0; i < cell.Pixels.Length; i++)
        {
            var (cr, cg, cb, ca) = MaterialPalette.OverlayColorOf(cell.Pixels[i]);
            canvas.Blend(i, cr, cg, cb, ca);
        }
    }
}
