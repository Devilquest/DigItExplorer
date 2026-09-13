using System.Buffers.Binary;

namespace DigItExplorer.Core.Catalog;

/// <summary>Provides direct byte reads at fixed file offsets from executable binaries (e.g. <c>MAIN.EXE</c>).</summary>
internal sealed class GameExecutable
{
    private readonly byte[] _bytes;

    private GameExecutable(byte[] bytes) => _bytes = bytes;

    /// <summary>Opens <paramref name="fileName"/> (e.g. <c>"MAIN.EXE"</c>) from <paramref name="gameDir"/>.</summary>
    public static GameExecutable Open(string gameDir, string fileName) =>
        new(File.ReadAllBytes(Path.Combine(gameDir, fileName)));

    /// <summary>Reads a span of bytes from the specified file offset.</summary>
    public ReadOnlySpan<byte> Read(int offset, int length) => _bytes.AsSpan(offset, length);

    /// <summary>Determines whether the specified range falls entirely within the executable bounds.</summary>
    public bool Covers(int offset, int length) =>
        offset >= 0 && length >= 0 && offset <= _bytes.Length - length;

    /// <summary>Reads a little-endian 16-bit word, failing when the offset is out of bounds.</summary>
    public bool TryReadWord(int offset, out int value)
    {
        value = 0;
        if (!Covers(offset, 2)) return false;
        value = BinaryPrimitives.ReadUInt16LittleEndian(Read(offset, 2));
        return true;
    }
}
