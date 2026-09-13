using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace DigItExplorer.App.Views;

/// <summary>Inspector panel displaying metadata and properties for the selected item.</summary>
internal partial class InfoPane : UserControl
{
    /// <summary>Occurs when a link to a resource file is followed.</summary>
    internal event EventHandler<string>? FileRequested;

    /// <summary>Occurs when the offered way back is taken.</summary>
    internal event EventHandler? BackRequested;

    public InfoPane()
    {
        InitializeComponent();
    }

    /// <summary>Reports the resource file a followed link names.</summary>
    private void FileLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Hyperlink { Tag: string resource }) FileRequested?.Invoke(this, resource);
    }

    /// <summary>Reports that the offered way back was taken.</summary>
    private void BackLink_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Opens the named tool's page in the system browser.</summary>
    private void ToolLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        e.Handled = true;
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
    }
}
