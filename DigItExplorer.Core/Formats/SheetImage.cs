namespace DigItExplorer.Core.Formats;

/// <summary>Represents a decoded SPF, MPF, or ANI image containing a VGA palette and one or more 320x200 frames.</summary>
public sealed class SheetImage
{
    // u16 extra_frames + u16 hint + a 768-byte palette, then the frame chain.
    private const int HeaderSize = 772;

    // Minimum possible frame footprint (u16 size + 1-byte opcode) used for header sanity checks.
    private const int SmallestFrame = 3;

    /// <summary>The sheet's embedded VGA palette.</summary>
    public VgaPalette Palette { get; }

    /// <summary>Frames in order; each is <see cref="FrameCodec.FrameBytes"/> palette-index bytes.</summary>
    public IReadOnlyList<byte[]> Frames { get; }

    /// <summary>Structural defect detected during decoding, if any.</summary>
    public SheetDefect Defect { get; }

    private SheetImage(VgaPalette palette, IReadOnlyList<byte[]> frames, SheetDefect defect)
    {
        Palette = palette;
        Frames = frames;
        Defect = defect;
    }

    /// <summary>Reads and decodes a sheet file from disk.</summary>
    public static SheetImage Read(string path) => Read(File.ReadAllBytes(path));

    /// <summary>Reads and decodes a sheet from its raw bytes.</summary>
    /// <param name="data">The raw sheet file bytes.</param>
    /// <returns>A decoded <see cref="SheetImage"/> with populated frames and defect status.</returns>
    public static SheetImage Read(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderSize)
            return new SheetImage(VgaPalette.From6Bit(stackalloc byte[768]), [], SheetDefect.NotASheet);

        var palette = VgaPalette.From6Bit(data.Slice(4, 768));
        var frames = new List<byte[]>();

        // The header's word counts the frames past the first, so the total is one more.
        int declaredFrames = (data[0] | (data[1] << 8)) + 1;
        var defect = data.Length < HeaderSize + declaredFrames * SmallestFrame
            ? SheetDefect.ImpossibleFrameCount
            : SheetDefect.None;

        // Reuse working buffer across frames so delta 'skip' opcodes retain previous frame pixels.
        var working = new byte[FrameCodec.WorkingSize];

        int off = HeaderSize;
        while (off + 2 <= data.Length)
        {
            int size = data[off] | (data[off + 1] << 8);
            off += 2;
            if (size == 0 || off + size > data.Length)
            {
                defect = SheetDefect.TruncatedChain;
                break;
            }

            if (!FrameCodec.Decompress(data.Slice(off, size), working))
            {
                defect = SheetDefect.UndecodableFrame;
                break;
            }

            frames.Add(working.AsSpan(0, FrameCodec.FrameBytes).ToArray());
            off += size;
        }

        return new SheetImage(palette, frames, defect);
    }
}
