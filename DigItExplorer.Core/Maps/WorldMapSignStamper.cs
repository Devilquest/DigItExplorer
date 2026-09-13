using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Core.Maps;

/// <summary>Stamps status icon signpost art at world map node anchor coordinates.</summary>
internal static class WorldMapSignStamper
{
    private const int CellStrideX = 29;
    private const int CellWidth = CellStrideX - 1; // 28
    private const int CellHeight = 27;

    /// <summary>Gets the pixel offset from a node anchor to the top-left corner of its sign art.</summary>
    public static (int Dx, int Dy) OffsetFor(World world) => world == World.Water ? (-4, -2) : (-12, -20);

    /// <summary>Slices a specific signpost type cell out of a decoded sign sheet frame.</summary>
    public static SpriteCell SliceCell(byte[] sgnFrame, SignType type)
    {
        int x1 = (int)type * CellStrideX + 1;
        return SpriteSheetSlicer.RectCell(sgnFrame, x1, 1, x1 + CellWidth - 1, CellHeight);
    }

    /// <summary>Whether a sliced sign pixel is drawn rather than left through: not transparent index 0 and
    /// not the sheet's own separator value.</summary>
    internal static bool IsDrawn(byte value, byte separator) => value != 0 && value != separator;

    /// <summary>Stamps signpost art for active nodes onto the RGB canvas in place.</summary>
    public static void Stamp(byte[] rgb, byte[] alpha, int width, int height, WorldMapPath path, World world,
        Func<int, SignType> signOf, byte[] sgnFrame, VgaPalette palette, IReadOnlySet<SignType> visibleTypes)
    {
        byte separator = sgnFrame[0];
        var pal = palette.Rgb;
        var (dx, dy) = OffsetFor(world);

        foreach (var (index, (nodeX, nodeY)) in path.Nodes)
        {
            var type = signOf(index);
            if (!visibleTypes.Contains(type)) continue;

            var cell = SliceCell(sgnFrame, type);

            foreach (var (dst, v) in new ClippedBlit(cell, nodeX + dx, nodeY + dy, width, height))
            {
                if (!IsDrawn(v, separator)) continue;

                RgbCanvas.PaintIndex(rgb, alpha, dst, pal, v);
            }
        }
    }
}
