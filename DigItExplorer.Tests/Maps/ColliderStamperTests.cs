using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Tests;

/// <summary>Guards how a drain's collider template reads its material codes.</summary>
public sealed class ColliderStamperTests
{
    private const int FrameWidth = 320;
    private const int FrameHeight = 200;
    private const byte Surface = 64;     // the one code that is real collision
    private const byte Open = 15;
    private const byte Border = 255;

    // Everything inside the cell that is not the surface: solid mass, open void, the wall codes a level
    // paints elsewhere, and the one code with no established role.
    private static readonly byte[] Shading = [0, 15, 32, 48, 96];

    [Fact]
    public void Only_the_walkable_surface_survives_as_collision()
    {
        var pixels = Collider().Pixels;

        Assert.Contains(Surface, pixels);
        // A wall code left in place would say the entrance is sealed, over ground that is solid anyway.
        Assert.All(pixels, code => Assert.True(code is Surface or Open or Border, $"unexpected code {code}"));
    }

    [Fact]
    public void The_border_is_passed_over_rather_than_opened()
    {
        var stamp = Stamp();

        // Rewriting the border to the open code would punch a hole through whatever the level put there,
        // so it stays as itself and is declared transparent instead.
        Assert.Contains(Border, stamp.Cell.Pixels);
        Assert.Equal(Border, stamp.TransparentCode);
    }

    [Fact]
    public void A_level_with_no_drain_in_it_resolves_nothing()
        => Assert.Empty(ColliderStamper.BuildDrainStamps([Record(0x02)], Sheet(TemplatePixels())));

    [Fact]
    public void A_mirrored_record_is_carried_through_to_the_stamp()
    {
        var stamps = ColliderStamper.BuildDrainStamps([Record(0x5B, p0: 0xFFFF)], Sheet(TemplatePixels()));

        Assert.True(Assert.Single(stamps).Mirrored);
    }

    private static SpriteCell Collider() => Stamp().Cell;

    private static ColliderStamp Stamp()
        => Assert.Single(ColliderStamper.BuildDrainStamps([Record(0x5B)], Sheet(TemplatePixels())));

    private static DlfRecord Record(byte category, ushort p0 = 1)
        => new(category, Type: 0, X: 0, Y: 0, P0: p0, P1: 0, P2: 0, P3: 0, P4: 0);

    // The collider template sits at (1,1)-(89,50). Every code the real sheet can hold is laid into it, one
    // per column, so a rule that spared the wrong one shows up as that code surviving into the cell.
    private static byte[] TemplatePixels()
    {
        var pixels = new byte[FrameWidth * FrameHeight];
        for (int y = 1; y <= 50; y++)
        {
            for (int x = 1; x <= 89; x++)
            {
                int column = x - 1;
                pixels[y * FrameWidth + x] = column switch
                {
                    0 => Border,
                    1 => Surface,
                    _ => Shading[column % Shading.Length],
                };
            }
        }
        return pixels;
    }

    // A one-frame sheet: u16 extra_frames, u16 hint, a 768-byte palette, then the frame behind its u16 size.
    private static byte[] Sheet(byte[] pixels)
    {
        var frame = Encode(pixels);
        var bytes = new List<byte> { 0, 0, 0, 0 };
        bytes.AddRange(new byte[768]);
        bytes.Add((byte)(frame.Length & 0xFF));
        bytes.Add((byte)(frame.Length >> 8));
        bytes.AddRange(frame);
        return [.. bytes];
    }

    // Skips over the zero runs and writes the rest literally. Literals alone would encode a 320x200 frame
    // in about 66,000 bytes, and the sheet states each frame's length in a u16.
    private static byte[] Encode(byte[] pixels)
    {
        var stream = new List<byte>();
        int i = 0;
        while (i < pixels.Length)
        {
            if (pixels[i] == 0)
            {
                int run = 0;
                while (i + run < pixels.Length && pixels[i + run] == 0 && run < 8192) run++;
                int encoded = run - 1;
                stream.Add((byte)(0x20 | (encoded >> 8)));      // op 1: skip, 13-bit count
                stream.Add((byte)(encoded & 0xFF));
                i += run;
            }
            else
            {
                int run = 0;
                while (i + run < pixels.Length && pixels[i + run] != 0 && run < 32) run++;
                stream.Add((byte)(run - 1));                    // op 0: copy N literal bytes
                stream.AddRange(pixels.AsSpan(i, run).ToArray());
                i += run;
            }
        }
        return [.. stream];
    }
}
