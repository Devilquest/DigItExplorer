using System.Windows;

namespace DigItExplorer.App.Views;


/// <summary>Custom modal message dialog for alerts and confirmations.</summary>
internal partial class MessageDialog : DialogWindow
{
    private MessageDialog(string title, string message, string? confirmLabel)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;

        if (confirmLabel is null) return;

        OkButton.Content = confirmLabel;
        OkButton.IsCancel = false;
        OkButton.Click += (_, _) => DialogResult = true;
        CancelButton.Visibility = Visibility.Visible;
    }

    /// <summary>Displays the message dialog modally over the owner window.</summary>
    public static void Show(Window owner, string title, string message)
        => new MessageDialog(title, message, confirmLabel: null) { Owner = owner }.ShowDialog();

    /// <summary>Displays the confirmation dialog modally over the owner window, and reports whether it was confirmed.</summary>
    public static bool Confirm(Window owner, string title, string message, string confirmLabel = "Replace")
        => new MessageDialog(title, message, confirmLabel) { Owner = owner }.ShowDialog() == true;
}
