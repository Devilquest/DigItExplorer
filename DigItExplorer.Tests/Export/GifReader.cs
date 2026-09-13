namespace DigItExplorer.Tests.Export;

/// <summary>One decoded frame of a GIF: its palette indices, and how long it is shown.</summary>
internal sealed record GifFrame(byte[] Pixels, int DelayCentiseconds, int Disposal, int? TransparentIndex);

/// <summary>Everything the tests read back out of an encoded GIF.</summary>
internal sealed record GifFile(int Width, int Height, byte[] Palette, int? LoopCount, List<GifFrame> Frames);

/// <summary>Strict GIF89a decoder for verifying GifEncoder outputs in test assertions.</summary>
internal static class GifReader
{
    /// <summary>Decodes a GIF89a byte array into a structured <see cref="GifFile"/> for test verification.</summary>
    public static GifFile Read(byte[] bytes)
    {
        int at = 0;

        Assert.Equal("GIF89a", System.Text.Encoding.ASCII.GetString(bytes, 0, 6));
        at = 6;

        int width = ReadUInt16(bytes, ref at);
        int height = ReadUInt16(bytes, ref at);
        byte packed = bytes[at++];
        at++; // background color index
        at++; // pixel aspect ratio

        Assert.True((packed & 0x80) != 0, "global color table flag");
        int colors = 2 << (packed & 0x07);
        var palette = bytes[at..(at + colors * 3)];
        at += colors * 3;

        int? loopCount = null;
        var frames = new List<GifFrame>();
        int pendingDelay = 0, pendingDisposal = 0;
        int? pendingTransparent = null;

        while (true)
        {
            byte block = bytes[at++];

            if (block == 0x3B) break; // trailer

            if (block == 0x21) // extension
            {
                byte label = bytes[at++];
                if (label == 0xF9) // graphic control
                {
                    Assert.Equal(4, bytes[at++]);
                    byte flags = bytes[at++];
                    pendingDisposal = (flags >> 2) & 0x07;
                    pendingDelay = ReadUInt16(bytes, ref at);
                    byte transparent = bytes[at++];
                    pendingTransparent = (flags & 1) != 0 ? transparent : null;
                    Assert.Equal(0, bytes[at++]);
                }
                else if (label == 0xFF) // application
                {
                    int nameLength = bytes[at++];
                    var name = System.Text.Encoding.ASCII.GetString(bytes, at, nameLength);
                    at += nameLength;
                    var data = ReadSubBlocks(bytes, ref at);
                    if (name == "NETSCAPE2.0") loopCount = data[1] | (data[2] << 8);
                }
                else
                {
                    ReadSubBlocks(bytes, ref at);
                }
                continue;
            }

            Assert.Equal(0x2C, block); // image descriptor
            at += 8;                   // left, top, width, height
            byte imagePacked = bytes[at++];
            Assert.Equal(0, imagePacked & 0x80); // no local color table
            Assert.Equal(0, imagePacked & 0x40); // not interlaced

            int minCodeSize = bytes[at++];
            var compressed = ReadSubBlocks(bytes, ref at);
            frames.Add(new GifFrame(Decompress(compressed, minCodeSize, width * height),
                pendingDelay, pendingDisposal, pendingTransparent));
        }

        return new GifFile(width, height, palette, loopCount, frames);
    }

    private static int ReadUInt16(byte[] bytes, ref int at)
    {
        int value = bytes[at] | (bytes[at + 1] << 8);
        at += 2;
        return value;
    }

    private static byte[] ReadSubBlocks(byte[] bytes, ref int at)
    {
        var data = new List<byte>();
        while (bytes[at] != 0)
        {
            int length = bytes[at++];
            data.AddRange(bytes[at..(at + length)]);
            at += length;
        }
        at++; // the empty block that closes the chain
        return [.. data];
    }

    /// <summary>Decompresses variable-length GIF LZW sub-blocks into indexed pixel bytes.</summary>
    private static byte[] Decompress(byte[] data, int minCodeSize, int expectedLength)
    {
        int clearCode = 1 << minCodeSize;
        int endCode = clearCode + 1;

        var dictionary = new List<byte[]>();
        var output = new List<byte>(expectedLength);

        int codeSize = 0, bits = 0, bitCount = 0, at = 0, previous = -1;

        Reset();

        while (true)
        {
            while (bitCount < codeSize && at < data.Length)
            {
                bits |= data[at++] << bitCount;
                bitCount += 8;
            }
            if (bitCount < codeSize) break;

            int code = bits & ((1 << codeSize) - 1);
            bits >>= codeSize;
            bitCount -= codeSize;

            if (code == clearCode)
            {
                Reset();
                previous = -1;
                continue;
            }
            if (code == endCode) break;

            byte[] entry;
            if (code < dictionary.Count) entry = dictionary[code];
            else if (code == dictionary.Count && previous >= 0)
                entry = [.. dictionary[previous], dictionary[previous][0]];
            else throw new InvalidDataException($"code {code} is not in the dictionary yet");

            output.AddRange(entry);

            if (previous >= 0 && dictionary.Count < 4096)
            {
                dictionary.Add([.. dictionary[previous], entry[0]]);
                if (dictionary.Count >= (1 << codeSize) && codeSize < 12) codeSize++;
            }

            previous = code;
        }

        return [.. output];

        void Reset()
        {
            dictionary.Clear();
            for (int i = 0; i < clearCode; i++) dictionary.Add([(byte)i]);
            dictionary.Add([]); // clear code, never looked up as an entry
            dictionary.Add([]); // end code
            codeSize = minCodeSize + 1;
        }
    }
}
