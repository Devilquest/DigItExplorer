namespace DigItExplorer.Core.Ui;

/// <summary>What a keystroke asks the application to do.</summary>
public enum ShortcutAction
{
    /// <summary>Steps the preview's magnification up.</summary>
    ZoomIn,

    /// <summary>Steps the preview's magnification down.</summary>
    ZoomOut,

    /// <summary>Scales the preview to the space available.</summary>
    FitToWindow,

    /// <summary>Returns the preview to one game pixel per screen pixel.</summary>
    ActualSize,

    /// <summary>Turns a whole layer group on or off, exactly as its own checkbox does.</summary>
    ToggleLayerGroup,

    /// <summary>Shows or hides the right side panel.</summary>
    TogglePanel,

    /// <summary>Toggles play or pause on the active media player.</summary>
    PlayPause,

    /// <summary>Toggles audio mute on or off.</summary>
    ToggleMute,
}

/// <summary>A layer group that answers to a key of its own.</summary>
public enum LayerShortcutGroup
{
    /// <summary>The collision plane and the footprints stamped into it.</summary>
    Collision,

    /// <summary>The enemies placed in the document.</summary>
    Enemies,

    /// <summary>The collectibles placed in the document.</summary>
    Goodies,
}

/// <summary>The action a keystroke names, and the layer group it names when the action needs one.</summary>
/// <param name="Action">What to do.</param>
/// <param name="LayerGroup">The group to turn, or null for every action but <see cref="ShortcutAction.ToggleLayerGroup"/>.</param>
public readonly record struct Shortcut(ShortcutAction Action, LayerShortcutGroup? LayerGroup = null);

/// <summary>One keystroke, as much of it as the shortcut table reads.</summary>
/// <param name="KeyName">The key's name as <c>System.Windows.Input.Key</c> spells it, which is how this
///   rule names keys without referencing WPF.</param>
/// <param name="Control">Whether a Ctrl key was held.</param>
/// <param name="Typing">Whether the focus is in a field that takes text.</param>
public readonly record struct ShortcutInput(string KeyName, bool Control, bool Typing);

/// <summary>One line of the shortcuts dialog: the keys as it writes them, and what they do.</summary>
/// <param name="Keys">The keystroke, spelled for a reader.</param>
/// <param name="Does">What pressing it does.</param>
public readonly record struct ShortcutHelp(string Keys, string Does);

/// <summary>What each keystroke means, and how the shortcuts dialog lists them.</summary>
public static class Shortcuts
{
    /// <summary>The action the given keystroke names, or null where it names none.</summary>
    public static Shortcut? Resolve(ShortcutInput input)
    {
        var framing = FramingOf(input.KeyName);

        if (input.Control) return ZoomOf(input.KeyName) ?? framing;
        if (input.Typing) return null;

        return framing ?? PlaybackOf(input.KeyName) ?? LetterOf(input.KeyName);
    }

    /// <summary>Every shortcut there is, in the words and the order the dialog lists them in.</summary>
    public static IReadOnlyList<ShortcutHelp> Help { get; } =
    [
        new("Ctrl + +", "Zoom In"),
        new("Ctrl + -", "Zoom Out"),
        new("0, Ctrl + 0", "Fit to View"),
        new("1, Ctrl + 1", "Original Size, 100%"),
        new("C", "Show/Hide the Collision Group"),
        new("E", "Show/Hide the Enemies Group"),
        new("G", "Show/Hide the Goodies Group"),
        new("P", "Show/Hide the Right Side Panel"),
        new("Space", "Play/Pause Media Playback"),
        new("M", "Mute/Unmute Audio"),
        new("Ctrl + O", "Open a Game Folder"),
        new("Ctrl + E", "Export What Is on Screen"),
        new("F1", "Open the User Guide"),
    ];

    private static Shortcut? ZoomOf(string keyName) => keyName switch
    {
        "OemPlus" or "Add" => new Shortcut(ShortcutAction.ZoomIn),
        "OemMinus" or "Subtract" => new Shortcut(ShortcutAction.ZoomOut),
        _ => null,
    };

    private static Shortcut? FramingOf(string keyName) => keyName switch
    {
        "D0" or "NumPad0" => new Shortcut(ShortcutAction.FitToWindow),
        "D1" or "NumPad1" => new Shortcut(ShortcutAction.ActualSize),
        _ => null,
    };

    private static Shortcut? PlaybackOf(string keyName) => keyName switch
    {
        "Space" => new Shortcut(ShortcutAction.PlayPause),
        "M" => new Shortcut(ShortcutAction.ToggleMute),
        _ => null,
    };

    private static Shortcut? LetterOf(string keyName) => keyName switch
    {
        "C" => new Shortcut(ShortcutAction.ToggleLayerGroup, LayerShortcutGroup.Collision),
        "E" => new Shortcut(ShortcutAction.ToggleLayerGroup, LayerShortcutGroup.Enemies),
        "G" => new Shortcut(ShortcutAction.ToggleLayerGroup, LayerShortcutGroup.Goodies),
        "P" => new Shortcut(ShortcutAction.TogglePanel),
        _ => null,
    };
}
