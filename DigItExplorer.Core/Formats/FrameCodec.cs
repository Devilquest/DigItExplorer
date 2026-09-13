namespace DigItExplorer.Core.Formats;

/// <summary>Decoder for the Dig It RLE/LZ frame compression shared by SPF, MPF, and ANI formats.</summary>
public static class FrameCodec
{
    /// <summary>Frame width in pixels.</summary>
    public const int Width = 320;

    /// <summary>Frame height in pixels.</summary>
    public const int Height = 200;

    /// <summary>Bytes in one fully-decoded frame.</summary>
    public const int FrameBytes = Width * Height;

    /// <summary>Headroom allocated for decompressor output before trimming to 64,000 bytes.</summary>
    public const int Slack = 0x4000;

    /// <summary>Minimum size for <see cref="Decompress"/>'s output buffer.</summary>
    public const int WorkingSize = FrameBytes + Slack;

    /// <summary>Decodes a compressed frame into <paramref name="outBuf"/>.</summary>
    /// <param name="src">Compressed frame byte stream.</param>
    /// <param name="outBuf">Target buffer for decompressed 320x200 linear VGA pixel indices.</param>
    /// <returns><c>true</c> if decompressed successfully; <c>false</c> if bounds checks failed.</returns>
    public static bool Decompress(ReadOnlySpan<byte> src, byte[] outBuf)
    {
        int di = 0, si = 0, n = src.Length, cap = outBuf.Length;
        while (di < FrameBytes && si < n)
        {
            int b = src[si];
            switch (b >> 5) // top 3 bits = opcode
            {
                case 0: // copy N literal bytes
                {
                    int cnt = (b & 0x1F) + 1;
                    si += 1;
                    if (si + cnt > n || di + cnt > cap) return false;
                    src.Slice(si, cnt).CopyTo(outBuf.AsSpan(di, cnt));
                    si += cnt; di += cnt;
                    break;
                }
                case 1: // skip N output bytes (13-bit, big-endian order)
                    if (si + 2 > n) return false;
                    di += (((b << 8) | src[si + 1]) & 0x1FFF) + 1;
                    si += 2;
                    break;
                case 2: // skip N output bytes (5-bit)
                    di += (b & 0x1F) + 1;
                    si += 1;
                    break;
                case 3: // write the next u16 value N times (count NOT +1)
                {
                    if (si + 4 > n) return false;
                    int cnt = ((b << 8) | src[si + 1]) & 0x1FFF;
                    si += 2;
                    if (di + 2 * cnt > cap) return false;
                    byte v0 = src[si], v1 = src[si + 1];
                    for (int k = 0; k < cnt; k++) { outBuf[di++] = v0; outBuf[di++] = v1; }
                    si += 2;
                    break;
                }
                case 4: // fill N bytes with the next byte (13-bit count)
                {
                    if (si + 3 > n) return false;
                    int cnt = (((b << 8) | src[si + 1]) & 0x1FFF) + 1;
                    si += 2;
                    if (di + cnt > cap) return false;
                    outBuf.AsSpan(di, cnt).Fill(src[si]);
                    si += 1; di += cnt;
                    break;
                }
                case 5: // fill N bytes with n1 (5-bit count)
                {
                    if (si + 2 > n) return false;
                    int cnt = (b & 0x1F) + 1;
                    if (di + cnt > cap) return false;
                    outBuf.AsSpan(di, cnt).Fill(src[si + 1]);
                    si += 2; di += cnt;
                    break;
                }
                case 6: // LZ: copy cnt bytes from out[di - dist] (may overlap → byte-by-byte)
                {
                    if (si + 3 > n) return false;
                    int dist = (((b << 8) | src[si + 1]) & 0x1FFF) + 1;
                    si += 2;
                    int cnt = src[si] + 1;
                    si += 1;
                    int bx = di - dist;
                    // A distance reaching back before the frame started indicates a corrupted or misaligned stream.
                    if (bx < 0 || bx + cnt > cap || di + cnt > cap) return false;
                    for (int k = 0; k < cnt; k++) outBuf[di++] = outBuf[bx++];
                    break;
                }
                default: // op 7: LZ absolute: copy cnt bytes from out[u16]
                {
                    if (si + 3 > n) return false;
                    int cnt = (b & 0x1F) + 1;
                    si += 1;
                    int bx = src[si] | (src[si + 1] << 8);
                    si += 2;
                    if (bx + cnt > cap || di + cnt > cap) return false;
                    for (int k = 0; k < cnt; k++) outBuf[di++] = outBuf[bx++];
                    break;
                }
            }
        }

        return true;
    }
}
