using System.Buffers.Binary;

namespace DigItExplorer.Core.Archives;

/// <summary>One directory entry inside an <see cref="XrsArchive"/>.</summary>
/// <param name="Name">File name (max 8.3 DOS name).</param>
/// <param name="Offset">Data offset, relative to the end of the directory table.</param>
/// <param name="Length">Data length in bytes.</param>
internal sealed record XrsEntry(string Name, uint Offset, uint Length);

/// <summary>Reads Pixel Painters .XRS archive directories and entries.</summary>
internal sealed class XrsArchive : IDisposable
{
    private readonly FileStream _stream;
    private readonly long _dataStart;

    /// <summary>Path of the archive on disk.</summary>
    public string Path { get; }

    /// <summary>Directory entries, in archive order.</summary>
    public IReadOnlyList<XrsEntry> Entries { get; }

    private XrsArchive(string path, FileStream stream, long dataStart, IReadOnlyList<XrsEntry> entries)
    {
        Path = path;
        _stream = stream;
        _dataStart = dataStart;
        Entries = entries;
    }

    /// <summary>Opens an archive and reads its directory (the data blob is read lazily by <see cref="Read"/>).</summary>
    public static XrsArchive Open(string path)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            Span<byte> header = stackalloc byte[2];
            stream.ReadExactly(header);
            int count = header[0] | (header[1] << 8);
            long dataStart = 2 + (long)count * 32;

            var dir = new byte[count * 32];
            stream.Position = 2;
            stream.ReadExactly(dir);

            var entries = new List<XrsEntry>(count);
            for (int i = 0; i < count; i++)
            {
                // 32-byte records: u8 name length, 12-byte name field, u32 offset, u32 length, 11 spare.
                int b = i * 32;
                int nameLen = Math.Min((int)dir[b], 12);
                string name = DecodeName(dir.AsSpan(b + 1, 12), nameLen);
                uint offset = BinaryPrimitives.ReadUInt32LittleEndian(dir.AsSpan(b + 13, 4));
                uint length = BinaryPrimitives.ReadUInt32LittleEndian(dir.AsSpan(b + 17, 4));
                entries.Add(new XrsEntry(name, offset, length));
            }
            return new XrsArchive(path, stream, dataStart, entries);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <summary>Reads and returns the raw bytes of an entry.</summary>
    public byte[] Read(XrsEntry entry)
    {
        var buffer = new byte[entry.Length];
        _stream.Position = _dataStart + entry.Offset;
        _stream.ReadExactly(buffer);
        return buffer;
    }

    // First len bytes of the 12-byte name field. ASCII-only: non-ASCII bytes are skipped to prevent malformed widening.
    private static string DecodeName(ReadOnlySpan<byte> field, int len)
    {
        Span<char> chars = stackalloc char[len];
        int n = 0;
        for (int i = 0; i < len; i++)
        {
            byte c = field[i];
            if (c < 0x80) chars[n++] = (char)c;
        }
        return new string(chars[..n]);
    }

    /// <summary>Closes the underlying file stream.</summary>
    public void Dispose() => _stream.Dispose();
}
