using System.Buffers.Binary;

namespace DigItExplorer.Core.Catalog;

/// <summary>One segment as the executable's own table declares it.</summary>
/// <param name="Base">The file offset the segment's bytes begin at.</param>
/// <param name="Length">The number of bytes the segment occupies in the file.</param>
internal readonly record struct NeSegment(int Base, int Length);

/// <summary>Parses the 16-bit New Executable (NE) segment table to compute segment file offsets.</summary>
internal static class NeSegmentTable
{
    // Field positions: the first is inside the MZ stub, the rest are relative to the NE header it points at.
    private const int NeHeaderPointer = 0x3C;      // u32 file offset of the NE header
    private const int SegmentCountField = 0x1C;
    private const int SegmentTableField = 0x22;    // itself relative to the NE header
    private const int AlignmentShiftField = 0x32;
    private const int RecordSize = 8;              // u16 sector; u16 length; u16 flags; u16 minalloc

    // NE format specifies a default alignment shift of 9 (512 bytes) when the field is 0.
    private const int DefaultAlignmentShift = 9;

    // Beyond this a shift is not a real alignment, and the multiply below would overflow rather than fail.
    private const int MaxAlignmentShift = 16;

    /// <summary>Reads where each segment begins and how long it is, in segment order.</summary>
    public static bool TryRead(GameExecutable exe, out NeSegment[] segments)
    {
        segments = [];
        if (!exe.Covers(NeHeaderPointer, 4)) return false;

        uint pointer = BinaryPrimitives.ReadUInt32LittleEndian(exe.Read(NeHeaderPointer, 4));
        if (pointer > int.MaxValue) return false;

        int header = (int)pointer;
        if (!exe.Covers(header, 2)) return false;

        var signature = exe.Read(header, 2);
        if (signature[0] != (byte)'N' || signature[1] != (byte)'E') return false;

        if (!exe.TryReadWord(header + SegmentCountField, out int count)) return false;
        if (!exe.TryReadWord(header + SegmentTableField, out int table)) return false;
        if (!exe.TryReadWord(header + AlignmentShiftField, out int shift)) return false;

        if (shift == 0) shift = DefaultAlignmentShift;
        if (count == 0 || shift > MaxAlignmentShift) return false;

        table += header;
        if (!exe.Covers(table, count * RecordSize)) return false;

        var result = new NeSegment[count];
        for (int i = 0; i < count; i++)
        {
            if (!exe.TryReadWord(table + i * RecordSize, out int sector)) return false;
            if (!exe.TryReadWord(table + i * RecordSize + 2, out int length)) return false;
            result[i] = new NeSegment(sector << shift, length);
        }

        segments = result;
        return true;
    }
}
