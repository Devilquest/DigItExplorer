using System.Windows;

namespace DigItExplorer.App.Views;

/// <summary>Modal dialog listing every keyboard shortcut the application answers to.</summary>
internal partial class ShortcutsWindow : DialogWindow
{
    private ShortcutsWindow() => InitializeComponent();

    /// <summary>Displays the shortcuts dialog modally over the owner window.</summary>
    public static void Show(Window owner) => new ShortcutsWindow { Owner = owner }.ShowDialog();
}
