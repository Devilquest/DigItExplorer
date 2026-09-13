using System.Buffers.Binary;

namespace DigItExplorer.Tests;

/// <summary>Builds an executable carrying nothing but an NE segment table, so a test can shape one like a
/// given build of the game with no copy of it present.</summary>
internal static class FakeMainExe
{
    /// <summary>Where the full release starts the four segments this application reads from, and how long
    /// its seg1 is, which is what tells the two builds apart.</summary>
    public const int FullSeg1 = 0x00F00, FullSeg2 = 0x05900, FullSeg3 = 0x0AC00, FullDGroup = 0x20100;

    /// <inheritdoc cref="FullSeg1"/>
    public const int FullSeg1Length = 15755;

    /// <summary>The same for the Manaccom edition, whose seg1 is 226 bytes longer and moves the rest on 512.</summary>
    public const int ManaccomSeg1 = 0x00F00, ManaccomSeg2 = 0x05B00, ManaccomSeg3 = 0x0AE00, ManaccomDGroup = 0x20300;

    /// <inheritdoc cref="ManaccomSeg1"/>
    public const int ManaccomSeg1Length = 15981;

    private const int HeaderAt = 0x80;
    private const int TableFromHeader = 0x40;
    private const int SegmentCount = 7;
    private const int AlignmentShift = 8;
    private const int RecordSize = 8;

    /// <summary>An executable shaped like the full release.</summary>
    public static byte[] FullRelease(int length = 0x21000)
        => WithSegments(FullSeg1, FullSeg2, FullSeg3, FullDGroup, FullSeg1Length, length);

    /// <summary>An executable shaped like the Manaccom edition.</summary>
    public static byte[] Manaccom(int length = 0x21000)
        => WithSegments(ManaccomSeg1, ManaccomSeg2, ManaccomSeg3, ManaccomDGroup, ManaccomSeg1Length, length);

    /// <summary>An executable whose segment table says exactly what it is told to say.</summary>
    public static byte[] WithSegments(int seg1, int seg2, int seg3, int dgroup, int seg1Length, int length,
        int segmentCount = SegmentCount)
    {
        var exe = new byte[length];

        BinaryPrimitives.WriteUInt32LittleEndian(exe.AsSpan(0x3C), HeaderAt);
        exe[HeaderAt] = (byte)'N';
        exe[HeaderAt + 1] = (byte)'E';
        Word(exe, HeaderAt + 0x1C, segmentCount);
        Word(exe, HeaderAt + 0x22, TableFromHeader);
        Word(exe, HeaderAt + 0x32, AlignmentShift);

        int table = HeaderAt + TableFromHeader;
        Segment(exe, table, 0, seg1, seg1Length);
        Segment(exe, table, 1, seg2, 0);
        Segment(exe, table, 2, seg3, 0);
        Segment(exe, table, 5, dgroup, 0);

        return exe;
    }

    /// <summary>Writes bytes at a seg1 offset, for a test placing an instruction where a build puts one.</summary>
    public static void PutInSeg1(byte[] exe, int seg1, int offset, params byte[] bytes)
        => bytes.CopyTo(exe.AsSpan(seg1 + offset));

    private static void Segment(byte[] exe, int table, int index, int start, int segmentLength)
    {
        Word(exe, table + (index * RecordSize), start >> AlignmentShift);
        Word(exe, table + (index * RecordSize) + 2, segmentLength);
    }

    private static void Word(byte[] bytes, int offset, int value)
        => BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), (ushort)value);
}
