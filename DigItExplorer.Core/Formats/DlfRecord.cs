using System.Buffers.Binary;

namespace DigItExplorer.Core.Formats;

/// <summary>An entity placement record from a <c>.DLF</c> level file.</summary>
/// <param name="Category">Entity category code.</param>
/// <param name="Type">Entity sub-type identifier within category.</param>
/// <param name="X">Signed horizontal coordinate in playfield pixel space.</param>
/// <param name="Y">Signed vertical coordinate in playfield pixel space.</param>
/// <param name="P0">Parameter word 0.</param>
/// <param name="P1">Parameter word 1.</param>
/// <param name="P2">Parameter word 2.</param>
/// <param name="P3">Parameter word 3.</param>
/// <param name="P4">Parameter word 4.</param>
public readonly record struct DlfRecord(byte Category, byte Type, short X, short Y,
    ushort P0, ushort P1, ushort P2, ushort P3, ushort P4)
{
    private const int RecordsOffset = 0x20;
    private const int RecordSize = 16;

    /// <summary>Reads all 16-byte entity records starting at offset 0x20 to the end of the file.</summary>
    /// <param name="dlf">Raw .DLF file bytes.</param>
    /// <returns>A list of decoded <see cref="DlfRecord"/> entries.</returns>
    public static IReadOnlyList<DlfRecord> ReadAll(byte[] dlf)
    {
        var records = new List<DlfRecord>();
        for (int off = RecordsOffset; off + RecordSize <= dlf.Length; off += RecordSize)
        {
            var span = dlf.AsSpan(off, RecordSize);
            records.Add(new DlfRecord(
                Category: span[0],
                Type: span[1],
                X: BinaryPrimitives.ReadInt16LittleEndian(span[2..]),
                Y: BinaryPrimitives.ReadInt16LittleEndian(span[4..]),
                P0: BinaryPrimitives.ReadUInt16LittleEndian(span[6..]),
                P1: BinaryPrimitives.ReadUInt16LittleEndian(span[8..]),
                P2: BinaryPrimitives.ReadUInt16LittleEndian(span[10..]),
                P3: BinaryPrimitives.ReadUInt16LittleEndian(span[12..]),
                P4: BinaryPrimitives.ReadUInt16LittleEndian(span[14..])));
        }
        return records;
    }
}
