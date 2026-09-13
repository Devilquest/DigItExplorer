using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge.Info;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.Tests;

/// <summary>Verifies diagnostic and repair note strings generated for damaged game resources.</summary>
public sealed class DamageNoteTests
{
    [Fact]
    public void A_sheet_that_read_cleanly_says_nothing()
    {
        Assert.Null(DamageNote.For(SheetDefect.None));
    }

    [Theory]
    [InlineData(SheetDefect.NotASheet)]
    [InlineData(SheetDefect.ImpossibleFrameCount)]
    [InlineData(SheetDefect.TruncatedChain)]
    [InlineData(SheetDefect.UndecodableFrame)]
    public void Every_defect_produces_a_diagnostic_note(SheetDefect defect)
    {
        var note = DamageNote.For(defect);

        Assert.NotNull(note);
        Assert.EndsWith(".", note);
    }

    [Fact]
    public void The_pieces_a_warning_is_completed_with_name_the_repair_tool()
    {
        var repair = DamageNote.RepairLead + DigItTools.Patcher.Name + DamageNote.RepairEnd;

        Assert.Equal(" Dig It! Patcher can fix it.", repair);
    }

    /// <summary>Ensures defect descriptions state tool availability without guaranteeing repair outcomes.</summary>
    [Theory]
    [InlineData(SheetDefect.NotASheet)]
    [InlineData(SheetDefect.ImpossibleFrameCount)]
    [InlineData(SheetDefect.TruncatedChain)]
    [InlineData(SheetDefect.UndecodableFrame)]
    public void No_defect_promises_that_anything_can_be_repaired(SheetDefect defect)
    {
        var note = DamageNote.For(defect)! + DamageNote.RepairLead + DigItTools.Patcher.Name
            + DamageNote.RepairEnd;

        Assert.DoesNotContain("will repair", note);
        Assert.DoesNotContain("can repair", note);
        Assert.DoesNotContain("restore", note);
    }

    /// <summary>Ensures header defect notes explain potential palette corruption caused by header offsets.</summary>
    [Fact]
    public void The_header_defect_gives_the_reason_the_colors_may_be_wrong()
    {
        var note = DamageNote.For(SheetDefect.ImpossibleFrameCount)!;

        Assert.Contains("header is invalid", note);
        Assert.Contains("may be wrong", note);
        Assert.Contains("immediately after the header", note);
    }

    [Fact]
    public void A_level_with_every_block_in_place_says_nothing()
    {
        Assert.Null(DamageNote.ForLayers(("Terrain", 0, 12), ("Collision", 0, 12)));
    }

    [Fact]
    public void A_level_names_the_damaged_layer_and_leaves_the_others_out_of_it()
    {
        var note = DamageNote.ForLayers(("Terrain", 0, 12), ("Collision", 11, 14))!;

        Assert.Contains("The Collision layer is missing 11 of its 14 blocks", note);
        Assert.DoesNotContain("Terrain", note);
    }

    [Fact]
    public void A_level_with_two_damaged_layers_names_both()
    {
        var note = DamageNote.ForLayers(("Terrain", 2, 12), ("Collision", 11, 14))!;

        Assert.Contains("The Terrain layer is missing 2 of its 12 blocks", note);
        Assert.Contains("The Collision layer is missing 11 of its 14 blocks", note);
    }
}
