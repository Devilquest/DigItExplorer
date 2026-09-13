using System.IO;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Export;

/// <summary>Encodes indexed animation frames into animated GIF89a streams using LZW compression.</summary>
public static class GifEncoder
{
    private const int ColorTableSize = 256;
    private const int MinCodeSize = 8;            // one code per palette index
    private const int ClearCode = 1 << MinCodeSize;
    private const int EndCode = ClearCode + 1;
    private const int MaxCode = 1 << 12;          // the format's ceiling on dictionary size

    /// <summary>Writes an animated GIF89a stream from indexed frames, palette, and timing parameters.</summary>
    public static void Write(Stream output, IReadOnlyList<byte[]> frames, int width, int height,
        VgaPalette palette, double msPerFrame, byte transparentIndex = 0, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentOutOfRangeException.ThrowIfLessThan(frames.Count, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        for (int f = 0; f < frames.Count; f++)
        {
            if (frames[f].Length < width * height)
            {
                throw new ArgumentException(
                    $"Frame {f} holds {frames[f].Length} bytes, too few for {width}×{height}.", nameof(frames));
            }
        }

        var delays = GifDelays.Centiseconds(frames.Count, msPerFrame);

        WriteHeader(output, width, height, palette);
        WriteLoopBlock(output, loop);

        for (int f = 0; f < frames.Count; f++)
        {
            WriteGraphicControl(output, delays[f], transparentIndex);
            WriteImageDescriptor(output, width, height);
            WriteImageData(output, frames[f], width * height);
        }

        output.WriteByte(0x3B); // trailer
    }

    private static void WriteHeader(Stream output, int width, int height, VgaPalette palette)
    {
        output.Write("GIF89a"u8);

        WriteUInt16(output, width);
        WriteUInt16(output, height);

        // Global color table present, 8 bits per color, 256 entries (the low three bits hold N where the
        // table has 2^(N+1) entries).
        output.WriteByte(0b1111_0111);
        output.WriteByte(0);  // background color index, unused: every frame covers the whole canvas
        output.WriteByte(0);  // no pixel aspect ratio given

        output.Write(palette.Rgb[..(ColorTableSize * 3)]);
    }

    /// <summary>Writes the Netscape 2.0 application extension block for looping.</summary>
    private static void WriteLoopBlock(Stream output, bool loop)
    {
        output.WriteByte(0x21);
        output.WriteByte(0xFF);
        output.WriteByte(11);
        output.Write("NETSCAPE2.0"u8);
        output.WriteByte(3);
        output.WriteByte(1);
        WriteUInt16(output, loop ? 0 : 1); // zero means forever
        output.WriteByte(0);
    }

    private static void WriteGraphicControl(Stream output, int delayCentiseconds, byte transparentIndex)
    {
        output.WriteByte(0x21);
        output.WriteByte(0xF9);
        output.WriteByte(4);

        // Disposal method 2 (restore to background) with transparency flag enabled.
        output.WriteByte((2 << 2) | 1);

        WriteUInt16(output, delayCentiseconds);
        output.WriteByte(transparentIndex);
        output.WriteByte(0);
    }

    private static void WriteImageDescriptor(Stream output, int width, int height)
    {
        output.WriteByte(0x2C);
        WriteUInt16(output, 0); // left
        WriteUInt16(output, 0); // top
        WriteUInt16(output, width);
        WriteUInt16(output, height);
        output.WriteByte(0);    // no local color table, not interlaced
    }

    private static void WriteUInt16(Stream output, int value)
    {
        output.WriteByte((byte)(value & 0xFF));
        output.WriteByte((byte)((value >> 8) & 0xFF));
    }

    /// <summary>LZW-compresses frame pixel indices into length-prefixed sub-blocks.</summary>
    private static void WriteImageData(Stream output, byte[] pixels, int count)
    {
        output.WriteByte(MinCodeSize);

        var blocks = new SubBlockWriter(output);
        int codeSize = MinCodeSize + 1;
        int nextCode = ClearCode + 2;
        var dictionary = new Dictionary<int, int>();

        Emit(ClearCode);

        int prefix = pixels[0];
        for (int i = 1; i < count; i++)
        {
            int pixel = pixels[i];
            int key = (prefix << 8) | pixel;

            if (dictionary.TryGetValue(key, out int known))
            {
                prefix = known;
                continue;
            }

            Emit(prefix);

            if (nextCode < MaxCode)
            {
                dictionary[key] = nextCode++;

                if (nextCode > (1 << codeSize) && codeSize < 12) codeSize++;
            }
            else
            {
                Emit(ClearCode);
                dictionary.Clear();
                nextCode = ClearCode + 2;
                codeSize = MinCodeSize + 1;
            }

            prefix = pixel;
        }

        Emit(prefix);
        Emit(EndCode);

        blocks.FlushBits();
        blocks.End();

        void Emit(int code) => blocks.WriteCode(code, codeSize);
    }

    /// <summary>Bit-packer and length-prefixed sub-block stream writer for GIF data.</summary>
    private sealed class SubBlockWriter(Stream output)
    {
        private readonly byte[] _block = new byte[255];
        private int _blockLength;
        private int _bits;
        private int _bitCount;

        public void WriteCode(int code, int codeSize)
        {
            _bits |= code << _bitCount;
            _bitCount += codeSize;

            while (_bitCount >= 8)
            {
                Add((byte)(_bits & 0xFF));
                _bits >>= 8;
                _bitCount -= 8;
            }
        }

        public void FlushBits()
        {
            if (_bitCount <= 0) return;
            Add((byte)(_bits & 0xFF));
            _bits = 0;
            _bitCount = 0;
        }

        public void End()
        {
            WriteBlock();
            output.WriteByte(0);
        }

        private void Add(byte value)
        {
            _block[_blockLength++] = value;
            if (_blockLength == _block.Length) WriteBlock();
        }

        private void WriteBlock()
        {
            if (_blockLength == 0) return;
            output.WriteByte((byte)_blockLength);
            output.Write(_block, 0, _blockLength);
            _blockLength = 0;
        }
    }
}
