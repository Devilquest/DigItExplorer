using System.Text;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Tests;

/// <summary>Verifies the ending sequence's captions as read out of the game's own draw calls.</summary>
public class EndSequenceCatalogTests
{
    /// <summary>Verifies that exactly ten ending sequence screens are defined.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Ten_screens_are_defined()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));
        Assert.Equal(10, data.EndSequence.Frames.Count);
    }

    /// <summary>Guards that caption draw calls resolve to valid text strings and on-screen coordinates.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Every_caption_resolves_to_text_at_an_on_screen_position()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        foreach (var frame in data.EndSequence.Frames)
        {
            Assert.NotEmpty(frame);
            foreach (var caption in frame)
            {
                Assert.NotEmpty(caption.Text);
                Assert.InRange(caption.X, 0, 320);
                Assert.InRange(caption.Y, 0, 200);
                Assert.InRange(caption.Style, 0, 15); // the ink-ramp table the style indexes holds 16 entries
            }
        }
    }

    /// <summary>Build fingerprint test verifying decoded end sequence captions against the reference build.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Captions_read_from_the_executable_match_the_known_build()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));
        var frames = data.EndSequence.Frames;

        Assert.Equal("ENDSEQ.MPF", data.EndSequence.FileName);
        Assert.Equal("TUNE7", data.EndSequence.TuneName);

        Assert.Equal(54, frames.Sum(f => f.Count));
        Assert.Equal([1, 6, 6, 8, 6, 6, 4, 6, 2, 9], frames.Select(f => f.Count));

        var first = Assert.Single(frames[0]);
        Assert.Equal("Dug and Dugette are together again.", Encoding.Latin1.GetString(first.Text));
        Assert.Equal((160, 180), (first.X, first.Y));
        Assert.Equal(TextAlign.Center, first.Align);
        Assert.Equal(0, first.Style);

        // The species names are drawn in the default ramp and their latin names in ramp 1: the one
        // per-screen ramp switch this sequence makes.
        Assert.Equal("Slugger", Encoding.Latin1.GetString(frames[1][0].Text));
        Assert.Equal(0, frames[1][0].Style);
        Assert.Equal("(Sluggi sloweus)", Encoding.Latin1.GetString(frames[1][3].Text));
        Assert.Equal(1, frames[1][3].Style);

        // The cheat-codes screen draws in ramp 5, and uses the font's one ink-free cell (0x1A) purely as a
        // 4-px indent, so its text is not all printable.
        Assert.Equal("Cheat Codes", Encoding.Latin1.GetString(frames[9][0].Text));
        Assert.Equal(5, frames[9][1].Style);
        Assert.Equal([0x1A, (byte)'X'], frames[9][2].Text);
    }
}
