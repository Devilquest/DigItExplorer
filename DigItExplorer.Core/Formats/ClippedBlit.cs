namespace DigItExplorer.Core.Formats;

/// <summary>One pixel of a blit: where it lands on the destination and the source palette index.</summary>
/// <param name="Dst">Destination pixel index, already row-major on the target's width.</param>
/// <param name="Value">Source palette index, before any transparency test.</param>
internal readonly record struct BlitPixel(int Dst, byte Value);

/// <summary>Walks a sprite cell's pixels in destination coordinates, yielding only those that land
/// inside the target and leaving the transparency test and the write to the caller.</summary>
internal struct ClippedBlit
{
    private readonly byte[] _pixels;
    private readonly int _cellW;
    private readonly int _cellH;
    private readonly int _x0;
    private readonly int _y0;
    private readonly int _targetW;
    private readonly int _targetH;
    private readonly bool _mirrored;
    private int _yy;
    private int _xx;

    /// <summary>Starts a blit of <paramref name="cell"/> with its top-left corner at
    /// (<paramref name="x0"/>, <paramref name="y0"/>).</summary>
    /// <param name="cell">The sprite cell being drawn.</param>
    /// <param name="x0">Destination x of the cell's left edge.</param>
    /// <param name="y0">Destination y of the cell's top edge.</param>
    /// <param name="targetW">Destination width in pixels.</param>
    /// <param name="targetH">Destination height in pixels.</param>
    /// <param name="mirrored">Whether the cell is drawn flipped horizontally.</param>
    public ClippedBlit(SpriteCell cell, int x0, int y0, int targetW, int targetH, bool mirrored = false)
    {
        _pixels = cell.Pixels;
        _cellW = cell.W;
        _cellH = cell.H;
        _x0 = x0;
        _y0 = y0;
        _targetW = targetW;
        _targetH = targetH;
        _mirrored = mirrored;
    }

    /// <summary>The pixel the last <see cref="MoveNext"/> landed on.</summary>
    public BlitPixel Current { get; private set; }

    /// <summary>Lets the blit drive a <c>foreach</c> without allocating.</summary>
    public readonly ClippedBlit GetEnumerator() => this;

    /// <summary>Advances to the next pixel that falls inside the destination.</summary>
    public bool MoveNext()
    {
        while (_yy < _cellH)
        {
            int ty = _y0 + _yy;
            if (ty >= 0 && ty < _targetH)
            {
                while (_xx < _cellW)
                {
                    int xx = _xx++;
                    int tx = _x0 + xx;
                    if (tx < 0 || tx >= _targetW) continue;

                    int sx = _mirrored ? _cellW - 1 - xx : xx;
                    Current = new BlitPixel(ty * _targetW + tx, _pixels[_yy * _cellW + sx]);
                    return true;
                }
            }
            _yy++;
            _xx = 0;
        }
        return false;
    }
}
