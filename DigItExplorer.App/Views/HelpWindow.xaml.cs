using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using DigItExplorer.App.Help;
using DigItExplorer.App.Interop;
using DigItExplorer.App.ViewModels;
using DigItExplorer.Core.Help;

namespace DigItExplorer.App.Views;

/// <summary>Non-modal single-instance window displaying help topics and a table of contents browser.</summary>
internal partial class HelpWindow : DialogWindow
{
    /// <summary>Singleton help window instance, or null when closed.</summary>
    private static HelpWindow? _open;

    private readonly HelpViewModel _model = new();
    private bool _syncing;

    private HelpWindow()
    {
        InitializeComponent();
        DataContext = _model;

        CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, (_, _) => SystemCommands.MaximizeWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, (_, _) => SystemCommands.RestoreWindow(this)));
        Reader.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Link_RequestNavigate));

        SourceInitialized += (_, _) =>
        {
            MaximizeBounds.Attach(this);
            CaptionButtonHitTest.Attach(this, MaximizeRestoreButton);
        };

        _model.Show(_model.Opening.Topic);
        Loaded += (_, _) => _model.Opening.IsSelected = true;
        Closed += (_, _) => _open = null;
    }

    /// <summary>Displays help window or brings existing instance to foreground.</summary>
    public static void Show(Window owner)
    {
        if (_open is not null)
        {
            if (_open.WindowState == WindowState.Minimized) _open.WindowState = WindowState.Normal;
            _open.Activate();
            return;
        }

        _open = new HelpWindow { Owner = owner };
        _open.Show();
    }

    private void Contents_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (_syncing || e.NewValue is not HelpEntry entry) return;

        GoTo(entry.Topic, entry.Anchor);
    }

    /// <summary>Handles hyperlink navigation requests within help topics.</summary>
    private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        e.Handled = true;

        var target = (e.Source as Hyperlink)?.Tag as string ?? e.Uri?.OriginalString;
        if (string.IsNullOrWhiteSpace(target)) return;

        if (target.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || target.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            return;
        }

        var parts = target.Split('#', 2);
        var file = parts[0].Length == 0 ? _model.CurrentTopic?.File : parts[0];
        var anchor = parts.Length == 2 ? parts[1] : null;

        if (file is not null && HelpContents.Find(file) is { } topic) GoTo(topic, anchor);
    }

    /// <summary>Navigates to the specified help topic and anchor.</summary>
    private void GoTo(HelpTopic topic, string? anchor)
    {
        bool changed = _model.Show(topic);

        if (anchor is not null) ScrollTo(anchor, changed);
        else if (changed) AfterLayout(ScrollToTop);

        Sync(topic, anchor);
    }

    /// <summary>Scrolls help topic document to beginning, which a kept document would otherwise reopen
    /// where it was last left.</summary>
    private void ScrollToTop() => _model.Document?.Blocks.FirstBlock?.BringIntoView();

    /// <summary>Scrolls help topic document to specified section anchor.</summary>
    private void ScrollTo(string anchor, bool afterPageChange)
    {
        if (afterPageChange)
        {
            // The new document is assigned but not laid out yet, and a block with no position cannot be
            // brought into view: asking now scrolls to where that heading sat in the previous page.
            AfterLayout(() => ScrollTo(anchor, afterPageChange: false));
            return;
        }

        if (_model.Document is { } document && HelpDocumentBuilder.HeadingAt(document, anchor) is { } heading)
        {
            heading.BringIntoView();
        }
    }

    /// <summary>Synchronizes table of contents selection with active topic anchor, falling back to the page
    /// itself for a heading too deep to appear in the contents.</summary>
    private void Sync(HelpTopic topic, string? anchor)
    {
        var page = _model.Contents.FirstOrDefault(entry => entry.Topic == topic);
        if (page is null) return;

        var destination = anchor is null
            ? page
            : page.Children.FirstOrDefault(child => child.Anchor == anchor) ?? page;

        _syncing = true;
        try
        {
            destination.IsSelected = true;
        }
        finally
        {
            _syncing = false;
        }
    }

    /// <summary>Executes action after layout pass completes.</summary>
    private void AfterLayout(Action action) => Dispatcher.BeginInvoke(DispatcherPriority.Loaded, action);

    private void ZoomIn_Click(object sender, RoutedEventArgs e) { Reader.IncreaseZoom(); UpdateZoomText(); }
    private void ZoomOut_Click(object sender, RoutedEventArgs e) { Reader.DecreaseZoom(); UpdateZoomText(); }

    private void UpdateZoomText() => ZoomText.Text = $"{Reader.Zoom:0}%";
}
