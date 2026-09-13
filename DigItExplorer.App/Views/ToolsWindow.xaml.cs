using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace DigItExplorer.App.Views;

/// <summary>Modal dialog listing the other tools built for Dig It!.</summary>
internal partial class ToolsWindow : DialogWindow
{
    private ToolsWindow() => InitializeComponent();

    /// <summary>Displays the tools dialog modally over the owner window.</summary>
    public static void Show(Window owner) => new ToolsWindow { Owner = owner }.ShowDialog();

    private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
