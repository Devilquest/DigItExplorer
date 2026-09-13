namespace DigItExplorer.Core.Formats;

/// <summary>Slices SPF/MPF sprite sheet frames into individual sprite cells using flood-fill and grid cropping.</summary>
internal static class SpriteSheetSlicer
{
    private const int Width = FrameCodec.Width;
    private const int Height = FrameCodec.Height;

    /// <summary>Flood-fills non-separator pixels across the frame to extract packed sprite cells.</summary>
    public static IReadOnlyList<SpriteCell> Slice(byte[] frame, byte separator)
        => Slice(frame, separator, 0, Height - 1);

    /// <summary>Flood-fills non-separator pixels within the inclusive row band <paramref name="yStart"/>..<paramref name="yEnd"/>.</summary>
    public static IReadOnlyList<SpriteCell> Slice(byte[] frame, byte separator, int yStart, int yEnd)
    {
        var seen = new bool[Width * Height];
        var cells = new List<SpriteCell>();
        var stack = new Stack<int>();

        for (int y0 = yStart; y0 <= yEnd; y0++)
        for (int x0 = 0; x0 < Width; x0++)
        {
            int start = y0 * Width + x0;
            if (seen[start] || frame[start] == separator) continue;

            stack.Clear();
            stack.Push(start);
            seen[start] = true;
            int minX = Width, minY = Height, maxX = -1, maxY = -1;
            var values = new HashSet<byte>();

            while (stack.Count > 0)
            {
                int p = stack.Pop();
                int y = p / Width, x = p % Width;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
                values.Add(frame[p]);

                if (x > 0 && !seen[p - 1] && frame[p - 1] != separator) { seen[p - 1] = true; stack.Push(p - 1); }
                if (x < Width - 1 && !seen[p + 1] && frame[p + 1] != separator) { seen[p + 1] = true; stack.Push(p + 1); }
                if (y > yStart && !seen[p - Width] && frame[p - Width] != separator) { seen[p - Width] = true; stack.Push(p - Width); }
                if (y < yEnd && !seen[p + Width] && frame[p + Width] != separator) { seen[p + Width] = true; stack.Push(p + Width); }
            }

            if (values.Count < 2) continue; // uniform region = packing filler, not a sprite

            int w = maxX - minX + 1, h = maxY - minY + 1;
            var pixels = new byte[w * h];
            for (int yy = 0; yy < h; yy++)
                Array.Copy(frame, (minY + yy) * Width + minX, pixels, yy * w, w);

            cells.Add(new SpriteCell(minX, minY, w, h, pixels));
        }

        cells.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
        return cells;
    }

    /// <summary>Crops a cell by index from a row-major uniform registration grid spanning sheet pages.</summary>
    public static SpriteCell GridCell(IReadOnlyList<byte[]> pages, int cols, int strideX, int strideY,
        int w, int h, int index)
    {
        int x = index % cols * strideX, y = index / cols * strideY;
        int page = 0;
        while (y + h > Height && page + 1 < pages.Count)
        {
            page++;
            y -= Height / strideY * strideY;
        }

        var frame = pages[page];
        var pixels = new byte[w * h];
        int copyW = Math.Min(w, Width - x);
        if (copyW > 0)
        {
            for (int yy = 0; yy < h && y + yy < Height; yy++)
                Array.Copy(frame, (y + yy) * Width + x, pixels, yy * w, copyW);
        }
        return new SpriteCell(x, y, w, h, pixels);
    }

    /// <summary>Crops one literal INCLUSIVE rect (x1..x2, y1..y2), for cells the game hardcodes rather than packs.</summary>
    public static SpriteCell RectCell(byte[] frame, int x1, int y1, int x2, int y2)
    {
        int w = x2 - x1 + 1, h = y2 - y1 + 1;
        var pixels = new byte[w * h];
        for (int yy = 0; yy < h; yy++)
            Array.Copy(frame, (y1 + yy) * Width + x1, pixels, yy * w, w);
        return new SpriteCell(x1, y1, w, h, pixels);
    }

    /// <summary>Extracts the top half of a vertically split sprite cell.</summary>
    public static SpriteCell TopHalf(SpriteCell cell)
    {
        int top = cell.H / 2;
        var pixels = new byte[cell.W * top];
        Array.Copy(cell.Pixels, 0, pixels, 0, cell.W * top);
        return cell with { H = top, Pixels = pixels };
    }

    /// <summary>Extracts the bottom half of a vertically split sprite cell.</summary>
    public static SpriteCell BottomHalf(SpriteCell cell)
    {
        int top = cell.H / 2;
        int bottom = cell.H - top;
        var pixels = new byte[cell.W * bottom];
        Array.Copy(cell.Pixels, top * cell.W, pixels, 0, cell.W * bottom);
        return cell with { Y = cell.Y + top, H = bottom, Pixels = pixels };
    }
}
