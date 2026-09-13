using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace DigItExplorer.App.Views;

/// <summary>Modal dialog displaying application version and licensing metadata.</summary>
internal partial class AboutWindow : DialogWindow
{
    private AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Version {ReadVersion()}";
    }

    /// <summary>Displays the About dialog modally over the owner window.</summary>
    public static void Show(Window owner) => new AboutWindow { Owner = owner }.ShowDialog();

    private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private static string ReadVersion()
        => typeof(AboutWindow).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(AboutWindow).Assembly.GetName().Version?.ToString()
            ?? "unknown";
}
