using DigItExplorer.Core.Export;

namespace DigItExplorer.Tests.Export;

/// <summary>Verifies export file name formatting, scaling suffixes, and character sanitization rules in <see cref="ExportFileName"/>.</summary>
public class ExportFileNameTests
{
    [Fact]
    public void Native_scale_leaves_the_name_alone()
        => Assert.Equal("Warm Up Run", ExportFileName.For("Warm Up Run", 1));

    [Theory]
    [InlineData(2, "Warm Up Run_2x")]
    [InlineData(4, "Warm Up Run_4x")]
    public void A_magnified_export_says_so(int scale, string expected)
        => Assert.Equal(expected, ExportFileName.For("Warm Up Run", scale));

    [Fact]
    public void The_breadcrumb_separator_becomes_a_dash()
        => Assert.Equal("Dry Lands - Warm Up Run", ExportFileName.For("Dry Lands · Warm Up Run", 1));

    /// <summary>Verifies that whitespace around breadcrumb separators is trimmed down to single spaces.</summary>
    [Fact]
    public void The_breadcrumbs_display_spacing_does_not_reach_the_file_name()
    {
        Assert.Equal("Dug - Dry Lands - Skid", ExportFileName.For("Dug    ·    Dry Lands    ·    Skid", 1));
        Assert.Equal("Spurk - Dry Lands - Retract", ExportFileName.For("Spurk    ·    Dry Lands    ·    Retract", 1));
    }

    [Fact]
    public void Whitespace_inside_a_name_is_left_as_one_space()
        => Assert.Equal("Great Waters - Swim", ExportFileName.For("  Great   Waters \t·\t Swim  ", 1));

    /// <summary>Verifies that file extensions in subject names are sanitized to prevent double extension suffixes.</summary>
    [Fact]
    public void A_subject_that_is_itself_a_file_name_does_not_keep_its_extension_as_one()
    {
        Assert.Equal("LVL000F_MPF", ExportFileName.For("LVL000F.MPF", 1));
        Assert.Equal("WO_DRG00_SPF_4x", ExportFileName.For("WO_DRG00.SPF", 4));
    }

    [Fact]
    public void Characters_a_file_system_rejects_are_replaced_rather_than_dropped()
    {
        // Kept distinct rather than collapsed away: two subjects differing only in punctuation must not
        // resolve to one file name.
        Assert.Equal("Flip It_ Cards", ExportFileName.For("Flip It: Cards", 1));
        Assert.Equal("a_b", ExportFileName.For("a/b", 1));
        Assert.NotEqual(ExportFileName.For("a/b", 1), ExportFileName.For("ab", 1));
    }

    /// <summary>Verifies that frame strip export names use a distinct frames suffix from single-frame exports.</summary>
    [Fact]
    public void A_strip_is_not_offered_the_same_name_as_a_single_frame()
    {
        Assert.NotEqual(ExportFileName.For("Walk", 1), ExportFileName.ForStrip("Walk", 1));
        Assert.Equal("Walk_frames", ExportFileName.ForStrip("Walk", 1));
        Assert.Equal("Walk_4x_frames", ExportFileName.ForStrip("Walk", 4));
    }

    [Fact]
    public void Frames_are_numbered_from_one()
    {
        Assert.Equal("Walk_1", ExportFileName.ForFrame("Walk", 1, 0, 9));
        Assert.Equal("Walk_9", ExportFileName.ForFrame("Walk", 1, 8, 9));
    }

    /// <summary>Verifies that exported frame numbers are zero-padded based on total frame count.</summary>
    [Theory]
    [InlineData(9, 0, "Walk_1")]
    [InlineData(12, 0, "Walk_01")]
    [InlineData(12, 11, "Walk_12")]
    [InlineData(100, 0, "Walk_001")]
    [InlineData(100, 99, "Walk_100")]
    public void Frame_numbers_are_padded_to_the_width_of_the_last_one(int frameCount, int index, string expected)
        => Assert.Equal(expected, ExportFileName.ForFrame("Walk", 1, index, frameCount));

    [Fact]
    public void The_scale_stays_ahead_of_the_frame_number()
        => Assert.Equal("Walk_4x_03", ExportFileName.ForFrame("Walk", 4, 2, 12));

    [Theory]
    [InlineData(0, 0)]   // no frames at all
    [InlineData(5, -1)]  // before the first
    [InlineData(5, 5)]   // past the last
    public void A_frame_outside_the_sequence_is_rejected(int frameCount, int index)
        => Assert.Throws<ArgumentOutOfRangeException>(() => ExportFileName.ForFrame("Walk", 1, index, frameCount));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_subject_with_nothing_in_it_still_names_a_file(string subject)
        => Assert.Equal("export", ExportFileName.For(subject, 1));

    [Fact]
    public void A_subject_of_nothing_but_bad_characters_keeps_their_replacements()
        => Assert.Equal("___", ExportFileName.For("///", 1));
}
