using DigItExplorer.Core.Ui;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="Shortcuts"/> answers every key the guide lists and nothing else.</summary>
public class ShortcutsTests
{
    private static Shortcut? Pressed(string keyName, bool control = false, bool typing = false)
        => Shortcuts.Resolve(new ShortcutInput(keyName, control, typing));

    private static Shortcut Group(LayerShortcutGroup group)
        => new(ShortcutAction.ToggleLayerGroup, group);

    [Fact]
    public void The_dialog_lists_every_key_in_the_order_it_shows_them()
    {
        // The window binds straight to this list and no test below reads it, so an emptied list would leave
        // a dialog with nothing in it and a suite with nothing to say about that.
        Assert.Equal(
            ["Ctrl + +", "Ctrl + -", "0, Ctrl + 0", "1, Ctrl + 1", "C", "E", "G", "P",
             "Space", "M", "Ctrl + O", "Ctrl + E", "F1"],
            Shortcuts.Help.Select(row => row.Keys));
    }

    [Fact]
    public void Zoom_steps_only_with_the_modifier_held()
    {
        Assert.Equal(new Shortcut(ShortcutAction.ZoomIn), Pressed("OemPlus", control: true));
        Assert.Equal(new Shortcut(ShortcutAction.ZoomOut), Pressed("OemMinus", control: true));
        Assert.Null(Pressed("OemPlus"));
        Assert.Null(Pressed("OemMinus"));
    }

    [Fact]
    public void The_numeric_keypad_zooms_like_the_number_row()
    {
        Assert.Equal(Pressed("OemPlus", control: true), Pressed("Add", control: true));
        Assert.Equal(Pressed("OemMinus", control: true), Pressed("Subtract", control: true));
    }

    [Fact]
    public void Framing_answers_with_the_modifier_and_without_it()
    {
        var fit = new Shortcut(ShortcutAction.FitToWindow);
        var actual = new Shortcut(ShortcutAction.ActualSize);

        Assert.Equal(fit, Pressed("D0"));
        Assert.Equal(fit, Pressed("D0", control: true));
        Assert.Equal(fit, Pressed("NumPad0"));
        Assert.Equal(fit, Pressed("NumPad0", control: true));
        Assert.Equal(actual, Pressed("D1"));
        Assert.Equal(actual, Pressed("D1", control: true));
        Assert.Equal(actual, Pressed("NumPad1"));
        Assert.Equal(actual, Pressed("NumPad1", control: true));
    }

    [Fact]
    public void Each_letter_names_the_group_or_the_panel_it_stands_for()
    {
        Assert.Equal(Group(LayerShortcutGroup.Collision), Pressed("C"));
        Assert.Equal(Group(LayerShortcutGroup.Enemies), Pressed("E"));
        Assert.Equal(Group(LayerShortcutGroup.Goodies), Pressed("G"));
        Assert.Equal(new Shortcut(ShortcutAction.TogglePanel), Pressed("P"));
    }

    [Fact]
    public void Playback_answers_to_space_and_m()
    {
        Assert.Equal(new Shortcut(ShortcutAction.PlayPause), Pressed("Space"));
        Assert.Equal(new Shortcut(ShortcutAction.ToggleMute), Pressed("M"));
    }

    [Fact]
    public void A_single_key_is_suspended_while_text_is_being_typed()
    {
        Assert.Null(Pressed("C", typing: true));
        Assert.Null(Pressed("E", typing: true));
        Assert.Null(Pressed("G", typing: true));
        Assert.Null(Pressed("P", typing: true));
        Assert.Null(Pressed("Space", typing: true));
        Assert.Null(Pressed("M", typing: true));
        Assert.Null(Pressed("D0", typing: true));
        Assert.Null(Pressed("D1", typing: true));
    }

    [Fact]
    public void A_combination_reaches_across_a_field_that_is_being_typed_in()
    {
        Assert.Equal(new Shortcut(ShortcutAction.ZoomIn), Pressed("OemPlus", control: true, typing: true));
        Assert.Equal(new Shortcut(ShortcutAction.FitToWindow), Pressed("D0", control: true, typing: true));
    }

    [Fact]
    public void The_modifier_leaves_the_letters_to_the_shortcuts_windows_already_owns()
    {
        // Ctrl+C, Ctrl+E, Ctrl+G and Ctrl+P belong to the field or the platform, not to the layer groups.
        Assert.Null(Pressed("C", control: true));
        Assert.Null(Pressed("E", control: true));
        Assert.Null(Pressed("G", control: true));
        Assert.Null(Pressed("P", control: true));
        Assert.Null(Pressed("Space", control: true));
        Assert.Null(Pressed("M", control: true));
    }

    [Fact]
    public void A_key_the_table_does_not_hold_names_nothing()
    {
        Assert.Null(Pressed("A"));
        Assert.Null(Pressed("D2"));
        Assert.Null(Pressed("F1"));
        Assert.Null(Pressed("Escape"));
        Assert.Null(Pressed("System"));
        Assert.Null(Pressed(""));
    }

    [Fact]
    public void Key_names_are_matched_as_the_key_enum_spells_them()
    {
        Assert.Null(Pressed("c"));
        Assert.Null(Pressed("oemplus", control: true));
    }
}
