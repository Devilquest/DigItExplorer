using System.Windows;
using System.Windows.Input;
using DigItExplorer.App.Interop;

namespace DigItExplorer.App.Views;

/// <summary>Base class for application dialog windows providing custom dark chrome.</summary>
internal abstract class DialogWindow : Window
{
    // Drawn here rather than using the platform dialog, which cannot be themed and whose buttons arrive in
    // the desktop's own language over a message written in English.
    protected DialogWindow()
    {
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(StyleProperty, "DialogWindowStyle");
        CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (_, _) => SystemCommands.CloseWindow(this)));

        SourceInitialized += (_, _) =>
        {
            DwmInterop.ApplyDarkTitleBar(this);
            DwmInterop.SquareOffCorners(this);
        };
    }
}
