namespace DigItExplorer.Core.Formats;

/// <summary>A rectangular sprite cell cropped from a 320x200 sheet frame.</summary>
public readonly record struct SpriteCell(int X, int Y, int W, int H, byte[] Pixels)
{
    /// <summary>Indicates whether every pixel in the cell is palette index 0 (transparent).</summary>
    public bool IsEmpty
    {
        get
        {
            foreach (var p in Pixels)
                if (p != 0) return false;
            return true;
        }
    }

    /// <summary>Indicates whether every pixel in the cell is index 0 or in the additional transparent set.</summary>
    public bool IsEmptyFor(IReadOnlySet<byte>? alsoTransparent)
    {
        if (alsoTransparent is null) return IsEmpty;
        foreach (var p in Pixels)
            if (p != 0 && !alsoTransparent.Contains(p)) return false;
        return true;
    }

    /// <summary>Stamps this cell on top of <paramref name="under"/> at offset (<paramref name="dx"/>, <paramref name="dy"/>).</summary>
    /// <param name="under">The base sprite cell beneath this cell.</param>
    /// <param name="dx">Horizontal placement offset.</param>
    /// <param name="dy">Vertical placement offset.</param>
    /// <param name="alsoTransparent">Optional additional palette indices treated as transparent.</param>
    /// <returns>A new <see cref="SpriteCell"/> with combined pixel data.</returns>
    public SpriteCell Over(SpriteCell under, int dx, int dy, IReadOnlySet<byte>? alsoTransparent = null)
    {
        var pixels = (byte[])under.Pixels.Clone();
        foreach (var (dst, p) in new ClippedBlit(this, dx, dy, under.W, under.H))
        {
            if (p == 0 || alsoTransparent?.Contains(p) == true) continue;
            pixels[dst] = p;
        }
        return under with { Pixels = pixels };
    }
}
