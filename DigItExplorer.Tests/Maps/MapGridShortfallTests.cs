using DigItExplorer.Core.Maps;

namespace DigItExplorer.Tests;

/// <summary>Tests level grid reconstruction handling for sheets with block shortfalls or surpluses.</summary>
public sealed class MapGridShortfallTests
{
    // A 2x2 level: 640x400 logical pixels over four 320x200 blocks.
    private static readonly byte[] TwoByTwoDlf = Dlf(width: 640, height: 400);

    [Fact]
    public void A_sheet_short_of_its_grid_keeps_the_levels_own_shape()
    {
        var collision = CollisionCompositor.Compose(Sheet(Block(0x11), Block(0x22)), TwoByTwoDlf);

        // The level is 2x2 whether or not the file can fill it. Laying the two blocks it did decode out as a
        // 2x1 strip would be a different level, drawn as confidently as a correct one.
        Assert.Equal(2, collision.Cols);
        Assert.Equal(2, collision.Rows);
        Assert.Equal(640, collision.Width);
        Assert.Equal(400, collision.Height);
        Assert.Equal(4, collision.BlockCount);
        Assert.Equal(2, collision.MissingBlocks);
    }

    [Fact]
    public void The_blocks_that_decoded_keep_their_own_places_and_the_rest_are_left_alone()
    {
        var collision = CollisionCompositor.Compose(Sheet(Block(0x11), Block(0x22)), TwoByTwoDlf);

        Assert.Equal(0x11, At(collision, 0, 0));     // block 0: top left
        Assert.Equal(0x22, At(collision, 320, 0));   // block 1: top right
        Assert.Equal(0x00, At(collision, 0, 200));   // block 2: never supplied
        Assert.Equal(0x00, At(collision, 320, 200)); // block 3: never supplied
    }

    [Fact]
    public void A_complete_sheet_reports_no_shortfall()
    {
        var collision = CollisionCompositor.Compose(
            Sheet(Block(0x11), Block(0x22), Block(0x33), Block(0x44)), TwoByTwoDlf);

        Assert.Equal(0, collision.MissingBlocks);
        Assert.Equal(0x33, At(collision, 0, 200));
        Assert.Equal(0x44, At(collision, 320, 200));
    }

    /// <summary>Verifies that sheets with surplus blocks report no shortfall and constrain bounds to header dimensions.</summary>
    [Fact]
    public void A_sheet_carrying_more_blocks_than_the_grid_needs_reports_no_shortfall_either()
    {
        var collision = CollisionCompositor.Compose(
            Sheet(Block(0x11), Block(0x22), Block(0x33), Block(0x44), Block(0x55), Block(0x66)), TwoByTwoDlf);

        Assert.Equal(0, collision.MissingBlocks);
        Assert.Equal(4, collision.BlockCount);
        Assert.Equal(640 * 400, collision.MaterialCodes.Length);
    }

    /// <summary>Verifies that standalone sheets without DLF headers decode as a single complete row.</summary>
    [Fact]
    public void A_sheet_with_no_level_behind_it_is_a_single_row_and_misses_nothing()
    {
        var collision = CollisionCompositor.Compose(Sheet(Block(0x11), Block(0x22)), dlf: null);

        Assert.Equal(2, collision.Cols);
        Assert.Equal(1, collision.Rows);
        Assert.Equal(0, collision.MissingBlocks);
    }

    private static int At(CollisionImage image, int x, int y) => image.MaterialCodes[y * image.Width + x];

    // u16 count, u16 width, u16 height. The count is the entity-record count and has no say in the grid.
    private static byte[] Dlf(int width, int height) =>
        [0, 0, (byte)(width & 0xFF), (byte)(width >> 8), (byte)(height & 0xFF), (byte)(height >> 8)];

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
