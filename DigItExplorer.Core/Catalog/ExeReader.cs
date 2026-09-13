using System.Buffers.Binary;

namespace DigItExplorer.Core.Catalog;

/// <summary>A text draw call site (<c>push x; push y; mov di, string</c>) decoded from executable bytecode.</summary>
/// <param name="X">Horizontal coordinate operand.</param>
/// <param name="Y">Vertical coordinate operand.</param>
/// <param name="String">The address of the string the call draws.</param>
internal readonly record struct TextCallSite(int X, int Y, ExeAddress String);

/// <summary>Validated reader for strings, coordinates, opcodes, and immediates in <see cref="GameExecutable"/>.</summary>
internal sealed class ExeReader(GameExecutable exe, BuildLayout layout)
{
    private static readonly byte[] PushImm16 = [0x68];            // push imm16
    private static readonly byte[] CmpAxImm16 = [0x3D];           // cmp ax, imm16
    private static readonly byte[] MovDiImm16 = [0xBF];           // mov di, imm16
    private static readonly byte[] CmpCounterImm8 = [0x83, 0x3E, 0x5E, 0x7A]; // cmp word [0x7A5E], imm8
    private static readonly byte[] CmpLocalImm8 = [0x83, 0x7E, 0xFE];         // cmp word [bp-2], imm8
    private static readonly byte[] AddCaptionYImm8 = [0x83, 0x85, 0xAE, 0xFE]; // add word [bp-0x152], imm8
    private static readonly byte[] MovCaptionYImm16 = [0xC7, 0x85, 0xAE, 0xFE]; // mov word [bp-0x152], imm16

    /// <summary>Where <paramref name="address"/> is in the build this reader was opened on.</summary>
    public int At(ExeAddress address) => layout.At(address);

    /// <summary>Whether the build this reader was opened on plays a distributor logo before the game's own
    /// title sequence, and so whether the sites inside that prologue are there to read.</summary>
    public bool PlaysLogoPrologue => layout.PlaysLogoPrologue;

    /// <summary>Reads a length-prefixed Pascal string from the executable.</summary>
    public bool TryReadPascalString(ExeAddress address, out byte[] text)
    {
        int offset = At(address);
        text = [];
        if (!exe.Covers(offset, 1)) return false;

        int length = exe.Read(offset, 1)[0];
        if (!exe.Covers(offset + 1, length)) return false;

        var bytes = exe.Read(offset + 1, length);
        // NUL characters indicate code/padding rather than valid Pascal text.
        if (bytes.Contains((byte)0)) return false;

        text = bytes.ToArray();
        return true;
    }

    /// <summary>Reads an 8-bit or 16-bit pushed immediate word.</summary>
    public bool TryReadPushedWord(ExeAddress address, out int value, out int length)
    {
        int offset = At(address);
        value = 0; length = 0;
        if (!exe.Covers(offset, 1)) return false;

        switch (exe.Read(offset, 1)[0])
        {
            case 0x6A when exe.Covers(offset + 1, 1):
                value = exe.Read(offset + 1, 1)[0]; length = 2; return true;
            case 0x68 when exe.Covers(offset + 1, 2):
                value = BinaryPrimitives.ReadUInt16LittleEndian(exe.Read(offset + 1, 2)); length = 3; return true;
            default:
                return false;
        }
    }

    /// <summary>Decodes a text-draw call site consisting of coordinates, string offset, and far call sequence.</summary>
    public bool TryReadTextCallSite(ExeAddress address, out TextCallSite site)
    {
        site = default;
        if (!TryReadPushedWord(address, out int x, out int xLength)) return false;
        if (!TryReadPushedWord(address + xLength, out int y, out int yLength)) return false;

        int cursor = At(address + xLength + yLength);
        if (!TryReadImmediate(cursor, MovDiImm16, 2, out int stringOffset)) return false;

        cursor += 3;
        if (!exe.Covers(cursor, 3)) return false;
        var tail = exe.Read(cursor, 3);
        if (tail[0] != 0x0E || tail[1] != 0x57 || tail[2] != 0x9A) return false; // push cs; push di; call far

        site = new TextCallSite(x, y, ExeAddress.FromOperand(address.Segment, stringOffset));
        return true;
    }

    /// <summary>Reads the operand of a <c>push imm16</c>.</summary>
    public bool TryReadPushImm16(ExeAddress address, out int value) => TryReadImmediate(At(address), PushImm16, 2, out value);

    /// <summary>Reads per-frame timer tick wait intervals from compare-and-branch instruction sequences.</summary>
    public bool TryReadTickWait(ExeAddress address, out int ticks)
    {
        int offset = At(address);
        ticks = 0;
        if (!TryReadImmediate(offset, CmpAxImm16, 2, out int compared)) return false;
        if (!exe.Covers(offset + 3, 1)) return false;

        switch (exe.Read(offset + 3, 1)[0])
        {
            case 0x72: ticks = compared; return true;     // jb, waits until the counter reaches it
            case 0x73: ticks = compared; return true;     // jae, the same threshold on the branch that leaves the loop
            case 0x76: ticks = compared + 1; return true; // jbe, waits until the counter passes it
            default: return false;
        }
    }

    /// <summary>Whether the bytes at <paramref name="address"/> are exactly <paramref name="expected"/>, for
    /// a caller validating the fixed parts of an instruction sequence it decodes itself.</summary>
    public bool Matches(ExeAddress address, ReadOnlySpan<byte> expected)
    {
        int offset = At(address);
        return exe.Covers(offset, expected.Length) && exe.Read(offset, expected.Length).SequenceEqual(expected);
    }

    /// <summary>Reads a plain little-endian word from a data table, no opcode to validate, so the only check
    /// is that it lies inside the file.</summary>
    public bool TryReadWord(ExeAddress address, out int value) => exe.TryReadWord(At(address), out value);

    /// <summary>Whether a range lies inside the file, for a caller bounds-checking a record before walking it.</summary>
    public bool Covers(ExeAddress address, int length) => exe.Covers(At(address), length);

    /// <summary>Reads the operand of a <c>mov di, imm16</c>: a string pointer handed to a draw or load call.</summary>
    public bool TryReadMovDiImm16(ExeAddress address, out int value) => TryReadImmediate(At(address), MovDiImm16, 2, out value);

    /// <summary>Reads that same operand as the address it is: one in the executable this reader is reading,
    /// so it is used exactly as found rather than adjusted the way an address of ours would be.</summary>
    public bool TryReadPointer(ExeAddress site, out ExeAddress target)
    {
        target = default;
        if (!TryReadMovDiImm16(site, out int offset)) return false;

        target = ExeAddress.FromOperand(site.Segment, offset);
        return true;
    }

    /// <summary>Reads the operand of a <c>cmp ax, imm16</c>: the value one arm of a dispatch branch tests for.</summary>
    public bool TryReadCmpAxImm16(ExeAddress address, out int value) => TryReadImmediate(At(address), CmpAxImm16, 2, out value);

    /// <summary>Reads the operand of a comparison against the intro player's own frame counter at
    /// <c>DS:0x7A5E</c> (how the caption branch encodes its iteration thresholds).</summary>
    public bool TryReadCounterThreshold(ExeAddress address, out int value) => TryReadImmediate(At(address), CmpCounterImm8, 1, out value);

    /// <summary>Reads the operand of a comparison against a loop's own stack counter at <c>[bp-2]</c>.</summary>
    public bool TryReadLocalThreshold(ExeAddress address, out int value) => TryReadImmediate(At(address), CmpLocalImm8, 1, out value);

    /// <summary>Reads the per-iteration addend of the caption's slide.</summary>
    public bool TryReadCaptionSlideStep(ExeAddress address, out int value) => TryReadImmediate(At(address), AddCaptionYImm8, 1, out value);

    /// <summary>Reads the caption slide's seeded starting y.</summary>
    public bool TryReadCaptionStartY(ExeAddress address, out int value) => TryReadImmediate(At(address), MovCaptionYImm16, 2, out value);

    private bool TryReadImmediate(int offset, ReadOnlySpan<byte> opcode, int operandSize, out int value)
    {
        value = 0;
        if (!exe.Covers(offset, opcode.Length + operandSize)) return false;
        if (!exe.Read(offset, opcode.Length).SequenceEqual(opcode)) return false;

        var operand = exe.Read(offset + opcode.Length, operandSize);
        value = operandSize == 1 ? operand[0] : BinaryPrimitives.ReadUInt16LittleEndian(operand);
        return true;
    }
}
