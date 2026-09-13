using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.App.Views;
using DigItExplorer.Core.Export;

namespace DigItExplorer.App.ViewModels;

/// <summary>ViewModel managing export commands, dialogs, and format serialization.</summary>
internal sealed partial class ExportViewModel : ObservableObject
{
    private readonly PreviewStage _stage;
    private readonly SessionSettings _settings;
    private readonly Func<string?> _describeIncludes;
    private readonly Action<string> _report;

    private ExportOffer? _offer;

    /// <param name="describeIncludes">What a layered document shows, or null for a preview with no layers,
    /// asked when the dialog opens because a document can render before its Layers tree exists.</param>
    /// <param name="report">Where a finished export announces itself: the window's status line.</param>
    internal ExportViewModel(PreviewStage stage, SessionSettings settings,
        Func<string?> describeIncludes, Action<string> report)
    {
        _stage = stage;
        _settings = settings;
        _describeIncludes = describeIncludes;
        _report = report;
    }

    /// <summary>Publishes an export offer for the current preview item.</summary>
    internal void Offer(ExportShape shape, string subject, int frameCount = 1,
        Func<int, BitmapSource>? frame = null, AnimationFrames? animation = null)
    {
        _offer = new ExportOffer(shape, subject, frameCount, frame, animation);
        ExportCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Withdraws the active export offer.</summary>
    internal void Withdraw()
    {
        _offer = null;
        ExportCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Whether an export offer is currently available.</summary>
    private bool CanExport => _offer is not null;

    /// <summary>Opens the export dialog and processes the selected export format.</summary>
    [RelayCommand(CanExecute = nameof(CanExport))]
    private void Export()
    {
        var image = _stage.CurrentImage;
        if (_offer is not { } offer || image is null) return;

        var owner = System.Windows.Application.Current.MainWindow;

        var choice = ExportDialog.Show(owner, offer, _describeIncludes(),
            image.PixelWidth, image.PixelHeight, _settings.ExportFormat, _settings.ExportScale);
        if (choice is null) return;

        _settings.ExportScale = choice.Scale;
        if (choice.FormatWasChosen) _settings.ExportFormat = choice.Format;

        switch (choice.Format)
        {
            case ExportFormat.FrameSequence: ExportFrameSequence(owner, offer, choice.Scale); break;
            case ExportFormat.FrameStrip: ExportFrameStrip(owner, offer, choice.Scale); break;
            case ExportFormat.AnimatedGif: ExportAnimatedGif(owner, offer, choice.Scale); break;
            default: ExportSingleImage(owner, image, offer.Subject, choice.Scale); break;
        }
    }

    /// <summary>Exports the current animation as an animated GIF file.</summary>
    private void ExportAnimatedGif(Window owner, ExportOffer offer, int scale)
    {
        if (offer.Animation is not { } animation) return;

        var save = new SaveFileDialog
        {
            Title = "Export Animation",
            Filter = "GIF image (*.gif)|*.gif",
            DefaultExt = ".gif",
            FileName = ExportFileName.For(offer.Subject, scale),
        };
        if (save.ShowDialog() != true) return;

        try
        {
            ImageExporter.WriteGif(animation, scale, save.FileName);
            _report($"Exported {animation.Steps.Count} steps to {save.FileName}");
        }
        catch (Exception ex)
        {
            MessageDialog.Show(owner, "Export Failed",
                $"The animation could not be written to:\n\n{save.FileName}\n\n{ex.Message}");
        }
    }

    /// <summary>Exports animation frames as a horizontal PNG strip image.</summary>
    private void ExportFrameStrip(Window owner, ExportOffer offer, int scale)
    {
        if (offer.Frame is not { } frameAt) return;

        var save = new SaveFileDialog
        {
            Title = "Export Frames",
            Filter = "PNG image (*.png)|*.png",
            DefaultExt = ".png",
            FileName = ExportFileName.ForStrip(offer.Subject, scale),
        };
        if (save.ShowDialog() != true) return;

        try
        {
            ImageExporter.WriteStripPng([.. Enumerable.Range(0, offer.FrameCount).Select(frameAt)],
                scale, save.FileName);
            _report($"Exported {offer.FrameCount} frames to {save.FileName}");
        }
        catch (Exception ex)
        {
            MessageDialog.Show(owner, "Export Failed",
                $"The frames could not be written to:\n\n{save.FileName}\n\n{ex.Message}");
        }
    }

    /// <summary>Exports the active preview frame as a PNG image file.</summary>
    private void ExportSingleImage(Window owner, BitmapSource image, string subject, int scale)
    {
        var save = new SaveFileDialog
        {
            Title = "Export Image",
            Filter = "PNG image (*.png)|*.png",
            DefaultExt = ".png",
            FileName = ExportFileName.For(subject, scale),
        };
        if (save.ShowDialog() != true) return;

        try
        {
            ImageExporter.WritePng(image, scale, save.FileName);
            _report($"Exported to {save.FileName}");
        }
        catch (Exception ex)
        {
            MessageDialog.Show(owner, "Export Failed",
                $"The image could not be written to:\n\n{save.FileName}\n\n{ex.Message}");
        }
    }

    /// <summary>Exports animation frames as individual numbered PNG files.</summary>
    private void ExportFrameSequence(Window owner, ExportOffer offer, int scale)
    {
        if (offer.Frame is not { } frameAt) return;

        var pick = new OpenFolderDialog { Title = "Select a Folder for the Frames" };
        if (pick.ShowDialog() != true) return;

        var paths = new string[offer.FrameCount];
        for (int i = 0; i < paths.Length; i++)
        {
            paths[i] = Path.Combine(pick.FolderName,
                ExportFileName.ForFrame(offer.Subject, scale, i, offer.FrameCount) + ".png");
        }

        int existing = paths.Count(File.Exists);
        if (existing > 0 && !MessageDialog.Confirm(owner, "Replace Existing Frames?",
                $"{existing} of the {paths.Length} frames already exist in:\n\n{pick.FolderName}\n\n" +
                "Exporting will replace them."))
        {
            return;
        }

        try
        {
            for (int i = 0; i < paths.Length; i++) ImageExporter.WritePng(frameAt(i), scale, paths[i]);
            _report($"Exported {paths.Length} frames to {pick.FolderName}");
        }
        catch (Exception ex)
        {
            MessageDialog.Show(owner, "Export Failed",
                $"The frames could not be written to:\n\n{pick.FolderName}\n\n{ex.Message}");
        }
    }
}
