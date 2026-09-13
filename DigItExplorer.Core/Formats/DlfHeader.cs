using System.Buffers.Binary;

namespace DigItExplorer.Core.Formats;

/// <summary>The header of a <c>LVLxxx.DLF</c> level file containing entity counts and dimensions.</summary>
/// <param name="Count">Highest entity-record index (total records = <c>Count + 1</c>).</param>
/// <param name="Width">Logical playfield width in pixels.</param>
/// <param name="Height">Logical playfield height in pixels.</param>
/// <param name="ParaSet">Parallax background set number, or 0 if omitted.</param>
/// <param name="TuneNo">Level background music track number, or 0 if omitted.</param>
/// <param name="IsBonus">Whether the level is a bonus zone rather than an ordinary substage.</param>
internal readonly record struct DlfHeader(int Count, int Width, int Height, int ParaSet, int TuneNo, bool IsBonus)
{
    /// <summary>Minimum bytes needed to read <see cref="Count"/>/<see cref="Width"/>/<see cref="Height"/>.</summary>
    public const int Size = 6;

    /// <summary>Bytes needed to also read <see cref="ParaSet"/> (header word 3).</summary>
    public const int SizeWithParaSet = 8;

    /// <summary>Bytes needed to also read <see cref="TuneNo"/> (header word 4).</summary>
    public const int SizeWithTune = 10;

    /// <summary>Bytes needed to also read <see cref="IsBonus"/> (header word 5).</summary>
    public const int SizeWithBonus = 12;

    /// <summary>Reads the header from the start of a <c>.DLF</c> file.</summary>
    public static DlfHeader Read(ReadOnlySpan<byte> dlf)
    {
        if (dlf.Length < Size)
            throw new ArgumentException($"DLF too small: need {Size} bytes, got {dlf.Length}.", nameof(dlf));
        int count = BinaryPrimitives.ReadUInt16LittleEndian(dlf);
        int width = BinaryPrimitives.ReadUInt16LittleEndian(dlf[2..]);
        int height = BinaryPrimitives.ReadUInt16LittleEndian(dlf[4..]);
        int paraSet = dlf.Length >= SizeWithParaSet ? BinaryPrimitives.ReadUInt16LittleEndian(dlf[6..]) : 0;
        int tuneNo = dlf.Length >= SizeWithTune ? BinaryPrimitives.ReadUInt16LittleEndian(dlf[8..]) : 0;
        bool isBonus = dlf.Length >= SizeWithBonus && BinaryPrimitives.ReadUInt16LittleEndian(dlf[10..]) != 0;
        return new DlfHeader(count, width, height, paraSet, tuneNo, isBonus);
    }
}
