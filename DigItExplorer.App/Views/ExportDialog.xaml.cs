using System.Windows;
using System.Windows.Controls;
using DigItExplorer.App.Models;

namespace DigItExplorer.App.Views;

/// <summary>Modal dialog prompting for image export format and scale options.</summary>
internal partial class ExportDialog : DialogWindow
{
    private readonly int _width, _height, _frameCount;

    private ExportFormat _format = ExportFormat.CurrentFrame;
    private int _scale = 1;

    /// <summary>Whether multiple export formats were available for selection.</summary>
    private bool HadFormatChoice { get; set; }

    private ExportDialog(ExportOffer offer, string? includes, int width, int height,
        ExportFormat initialFormat, int initialScale)
    {
        InitializeComponent();

        _width = width;
        _height = height;
        _frameCount = offer.FrameCount;

        SubjectText.Text = offer.Subject;

        var note = includes ?? NoteFor(offer);
        if (note is null) IncludesText.Visibility = Visibility.Collapsed;
        else IncludesText.Text = note;

        bool severalFrames = offer is { FrameCount: > 1, Frame: not null };

        if (severalFrames)
        {
            FormatSequence.Content = $"All {offer.FrameCount} Frames as Separate PNGs";
            FormatSequence.Visibility = Visibility.Visible;

            if (offer.Shape == ExportShape.Animation)
            {
                FormatStrip.Content = $"All {offer.FrameCount} Frames Side by Side as One PNG";
                FormatStrip.Visibility = Visibility.Visible;
            }
        }

        if (offer.Animation is not null) FormatGif.Visibility = Visibility.Visible;

        HadFormatChoice = severalFrames || offer.Animation is not null;
        if (HadFormatChoice)
        {
            SingleFormatText.Visibility = Visibility.Collapsed;
            FormatChoice.Visibility = Visibility.Visible;
        }

        Offered(initialFormat).IsChecked = true;
        (initialScale switch { 4 => Scale4, 2 => Scale2, _ => Scale1 }).IsChecked = true;
    }

    /// <summary>Retrieves radio button corresponding to format choice.</summary>
    private RadioButton Offered(ExportFormat format)
    {
        var wanted = format switch
        {
            ExportFormat.FrameSequence => FormatSequence,
            ExportFormat.FrameStrip => FormatStrip,
            ExportFormat.AnimatedGif => FormatGif,
            _ => FormatCurrent,
        };
        return wanted.Visibility == Visibility.Visible ? wanted : FormatCurrent;
    }

    /// <summary>Builds contextual descriptive note for export shapes.</summary>
    private static string? NoteFor(ExportOffer offer) => offer.Shape switch
    {
        ExportShape.SheetFrames when offer.FrameCount > 1 =>
            "The file's own frames, in the order they are stored, not one of the game's animations.",
        ExportShape.Animation when offer.FrameCount > 1 =>
            "The unique frames that make up this animation. Frames played more than once appear only once.",
        _ => null,
    };

    /// <summary>Displays the export options dialog modally over the owner window, returning the choice made, or null if it was canceled.</summary>
    internal static ExportChoice? Show(Window owner, ExportOffer offer, string? includes,
        int width, int height, ExportFormat initialFormat, int initialScale)
    {
        var dialog = new ExportDialog(offer, includes, width, height, initialFormat, initialScale)
        {
            Owner = owner,
        };
        return dialog.ShowDialog() == true
            ? new ExportChoice(dialog._format, dialog._scale, dialog.HadFormatChoice)
            : null;
    }

    private void Format_Checked(object sender, RoutedEventArgs e)
    {
        _format =
            ReferenceEquals(sender, FormatSequence) ? ExportFormat.FrameSequence
            : ReferenceEquals(sender, FormatStrip) ? ExportFormat.FrameStrip
            : ReferenceEquals(sender, FormatGif) ? ExportFormat.AnimatedGif
            : ExportFormat.CurrentFrame;

        ShowSize();
    }

    private void Scale_Checked(object sender, RoutedEventArgs e)
    {
        _scale = ReferenceEquals(sender, Scale4) ? 4 : ReferenceEquals(sender, Scale2) ? 2 : 1;
        ShowSize();
    }

    /// <summary>Updates computed pixel dimensions text for selected format and scale.</summary>
    private void ShowSize()
    {
        bool isStrip = _format == ExportFormat.FrameStrip;
        int width = isStrip ? _width * _frameCount : _width;

        var size = _scale == 1
            ? $"{width}×{_height}"
            : $"{width}×{_height}  →  {width * _scale}×{_height * _scale}";

        SizeText.Text =
            isStrip ? $"{size}  ({_width * _scale}×{_height * _scale} each)"
            : _format == ExportFormat.FrameSequence ? $"{size}  each"
            : size;
    }

    private void Export_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
