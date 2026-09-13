using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.Tests;

/// <summary>Tests error handling, defect reporting, and partial decoding in <see cref="SheetImage.Read(ReadOnlySpan{byte})"/>.</summary>
public sealed class SheetDefectTests
{
    /// <summary>Verifies that sheets from an undamaged copy of the game report no defects and decode without errors.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.MeasuredArchives)]
    public void Nothing_in_an_undamaged_copy_is_reported_as_damaged()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        Assert.True(ArchiveFingerprints.TryIdentify(out var expected));

        using var library = ResourceLibrary.Open(gameDir);
        var checked_ = 0;

        foreach (var name in library.Names.Where(ResourceLibrary.IsRenderable))
        {
            var sheet = SheetImage.Read(library.Read(name));
            Assert.Equal(SheetDefect.None, sheet.Defect);
            Assert.NotEmpty(sheet.Frames);
            checked_++;
        }

        // Guards the guard: a lookup that silently matched nothing would pass the loop above.
        Assert.Equal(expected.Sheets, checked_);
    }

    [Fact]
    public void A_file_too_short_to_hold_a_header_is_not_a_sheet()
    {
        var sheet = SheetImage.Read(new byte[100]);

        Assert.Equal(SheetDefect.NotASheet, sheet.Defect);
        Assert.Empty(sheet.Frames);
    }

    [Fact]
    public void A_header_claiming_more_frames_than_the_file_can_hold_is_caught()
    {
        var sheet = SheetImage.Read(Sheet(declaredExtraFrames: 60_000, SkipOneByte));

        Assert.Equal(SheetDefect.ImpossibleFrameCount, sheet.Defect);
        // The header is what was damaged, so the frames behind it still decode.
        Assert.Single(sheet.Frames);
    }

    [Fact]
    public void A_frame_running_past_the_end_of_the_file_truncates_the_chain()
    {
        var sheet = SheetImage.Read([.. Sheet(declaredExtraFrames: 1, SkipOneByte), 0xFF, 0xFF]);

        Assert.Equal(SheetDefect.TruncatedChain, sheet.Defect);
        Assert.Single(sheet.Frames);
    }

    [Fact]
    public void An_opcode_reaching_outside_its_buffers_ends_the_frame()
    {
        // The back-reference opcode, asked to copy from one byte before the frame began. A displaced stream
        // produces exactly this: the operand was never a distance, it is whatever the misalignment landed on.
        var sheet = SheetImage.Read(Sheet(declaredExtraFrames: 0, [0xC0, 0x00, 0x00]));

        Assert.Equal(SheetDefect.UndecodableFrame, sheet.Defect);
        Assert.Empty(sheet.Frames);
    }

    [Fact]
    public void Everything_that_decoded_before_the_damage_is_kept()
    {
        var sheet = SheetImage.Read([.. Sheet(declaredExtraFrames: 3, SkipOneByte, SkipOneByte, SkipOneByte),
                                     0xFF, 0xFF]);

        Assert.Equal(SheetDefect.TruncatedChain, sheet.Defect);
        Assert.Equal(3, sheet.Frames.Count);
    }

    [Fact]
    public void A_broken_chain_is_reported_ahead_of_a_broken_header()
    {
        var sheet = SheetImage.Read([.. Sheet(declaredExtraFrames: 60_000, SkipOneByte), 0xFF, 0xFF]);

        Assert.Equal(SheetDefect.TruncatedChain, sheet.Defect);
    }

    // Opcode 1 with a count of one: the shortest stream that decodes without producing a whole frame. The
    // decode ends on running out of input, which is how the engine's own loop ends and is not a defect.
    private static readonly byte[] SkipOneByte = [0x20, 0x00];

    // A sheet file: u16 extra_frames, u16 hint, a 768-byte palette, then each frame behind its u16 size.
    private static byte[] Sheet(int declaredExtraFrames, params byte[][] frames)
    {
        var bytes = new List<byte>
        {
            (byte)(declaredExtraFrames & 0xFF), (byte)(declaredExtraFrames >> 8),
            0, 0,
        };
        bytes.AddRange(new byte[768]);

        foreach (var frame in frames)
        {
            bytes.Add((byte)(frame.Length & 0xFF));
            bytes.Add((byte)(frame.Length >> 8));
            bytes.AddRange(frame);
        }

        return [.. bytes];
    }
}
