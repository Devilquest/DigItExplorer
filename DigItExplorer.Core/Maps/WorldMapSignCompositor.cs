using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Core.Maps;

/// <summary>Composes a single world-map signpost cell as an isolated still.</summary>
public static class WorldMapSignCompositor
{
    /// <summary>Sign sheet filename for a world's world map.</summary>
    public static string Sheet(World world) => $"{GameKnowledge.WorldMapPrefix(world)}SGN.SPF";

    /// <summary>Palette filename the sign sheet is drawn through, shared with the world map itself.</summary>
    public static string Palette(World world) => $"{GameKnowledge.WorldMapPrefix(world)}.PAL";

    /// <summary>Composes one signpost type still for a world.</summary>
    public static SpriteRenderResult? Compose(Func<string, byte[]?> loadResource, World world, SignType type)
    {
        var sheetBytes = loadResource(Sheet(world));
        if (sheetBytes is null) return null;

        var palBytes = loadResource(Palette(world));
        if (palBytes is null || palBytes.Length < 768) return null;
        var palette = VgaPalette.From6Bit(palBytes.AsSpan(0, 768));

        var frame = SheetImage.Read(sheetBytes).Frames[0];
        var cell = WorldMapSignStamper.SliceCell(frame, type);
        byte separator = frame[0];

        var canvas = new SpriteCanvas(cell.W, cell.H);
        canvas.DrawIndexed(cell, palette, v => WorldMapSignStamper.IsDrawn(v, separator));

        return new SpriteRenderResult(canvas.Rgb, canvas.Alpha, cell.W, cell.H, ShowedGraphic: true,
            ShowedCollision: false);
    }
}
