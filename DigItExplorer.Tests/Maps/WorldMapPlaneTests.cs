using DigItExplorer.Core.Maps;

namespace DigItExplorer.Tests;

/// <summary>Guards the world map canvas width against a chain padded out with blank blocks.</summary>
public sealed class WorldMapPlaneTests
{
    private const int BlockWidth = 320;
    private const int BlockHeight = 200;

    [Fact]
    public void A_chain_padded_with_blank_blocks_is_as_wide_as_the_map_and_not_as_the_padding()
    {
        var (width, height, blocks, _) = WorldMapPlane.Decode(Sheet(Block(0x11), Block(0x22), Block(0), Block(0)));

        // Tiling the padding would carry the canvas out to four blocks, and the map would be shown with a
        // blank strip that the game never draws.
        Assert.Equal(2, blocks);
        Assert.Equal(2 * BlockWidth, width);
        Assert.Equal(BlockHeight, height);
    }

    [Fact]
    public void A_blank_block_between_two_drawn_ones_is_part_of_the_map()
    {
        // Only the tail is padding. A gap inside the chain is a screen the map really has.
        var (_, _, blocks, _) = WorldMapPlane.Decode(Sheet(Block(0x11), Block(0), Block(0x22)));

        Assert.Equal(3, blocks);
    }

    [Fact]
    public void A_chain_of_nothing_but_blanks_still_decodes_to_one_block()
    {
        var (width, _, blocks, _) = WorldMapPlane.Decode(Sheet(Block(0), Block(0)));

        // Trimming to nothing would throw on a file that decoded perfectly well.
        Assert.Equal(1, blocks);
        Assert.Equal(BlockWidth, width);
    }

    // Eight fills of 8192 bytes each: the shortest stream that produces a whole 64000-byte block, since the
    // fill opcode's count is 13 bits wide.
    private static byte[] Block(byte value)
    {
        var frame = new List<byte>();
        for (int i = 0; i < 8; i++) frame.AddRange([(byte)0x9F, (byte)0xFF, value]);
        return [.. frame];
    }

    // u16 extra_frames, u16 hint, a 768-byte palette, then each block behind its u16 size.
    private static byte[] Sheet(params byte[][] blocks)
    {
        var bytes = new List<byte> { (byte)(blocks.Length - 1), 0, 0, 0 };
        bytes.AddRange(new byte[768]);
        foreach (var block in blocks)
        {
            bytes.Add((byte)(block.Length & 0xFF));
            bytes.Add((byte)(block.Length >> 8));
            bytes.AddRange(block);
        }
        return [.. bytes];
    }
}
