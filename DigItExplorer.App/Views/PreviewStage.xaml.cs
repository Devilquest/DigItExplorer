using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DigItExplorer.App.Converters;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.App.Views;

/// <summary>Preview viewport hosting images, text, placeholders, and zoom controls.</summary>
internal partial class PreviewStage : UserControl
{
    private const double MinZoom = 0.1, MaxZoom = 32.0, ZoomStep = 1.25;

    private bool _hasImage;
    private double _zoom = 1.0;
    private int _imageWidth, _imageHeight;

    private bool _panning;
    private Point _panStart;
    private double _panStartH, _panStartV;

    private SheetImage? _currentSheet;
    private int _currentFrameIndex;

    private bool _isFitMode = true;
    private int _fitGeneration;

    /// <summary>Occurs when zoom level or fit-to-window state is modified.</summary>
    public event EventHandler? ZoomOrFitChanged;

    /// <summary>Occurs when the open game folder link on the welcome screen is clicked.</summary>
    public event EventHandler? OpenGameFolderRequested;

    /// <summary>Current zoom multiplier where 1.0 represents 100% scaling.</summary>
    public double Zoom => _zoom;

    /// <summary>Whether stage auto-fits the image to the viewport.</summary>
    public bool IsFitMode => _isFitMode;

    /// <summary>Active preview bitmap source at unscaled native resolution.</summary>
    public BitmapSource? CurrentImage => _hasImage ? PreviewImage.Source as BitmapSource : null;

    public PreviewStage()
    {
        InitializeComponent();
    }

    /// <summary>Displays a multi-frame sheet image and configures frame toolbar.</summary>
    public void ShowImage(SheetImage sheet, bool fit = true, double? zoomIfNotFit = null)
    {
        _currentSheet = sheet;
        _currentFrameIndex = 0;
        ShowBitmap(BitmapConverter.ToBitmap(sheet, frameIndex: 0), FrameCodec.Width, FrameCodec.Height, fit, zoomIfNotFit);
        UpdateFrameToolbar();
    }

    /// <summary>Displays an unscaled bitmap source on the preview canvas.</summary>
    public void ShowBitmap(BitmapSource bitmap, int width, int height, bool fit = true, double? zoomIfNotFit = null)
    {
        StageBackdrop.Background = Brushes.Transparent;
        PreviewImage.Source = bitmap;
        _imageWidth = width;
        _imageHeight = height;
        _hasImage = true;

        ImageScroll.Visibility = Visibility.Visible;
        ZoomToolbar.Visibility = Visibility.Visible;
        if (FrameToolbar != null) FrameToolbar.Visibility = Visibility.Collapsed;
        TextPreview.Visibility = Visibility.Collapsed;
        EmptyHint.Visibility = Visibility.Collapsed;
        NoGameHint.Visibility = Visibility.Collapsed;

        _isFitMode = fit;

        if (fit)
        {
            UpdateLayout();
            FitToWindow();
            FitWhenLayoutSettles();
        }
        else
        {
            _fitGeneration++;
            SetZoom(zoomIfNotFit ?? _zoom);
        }
    }

    /// <summary>Updates the active preview bitmap without changing viewport framing.</summary>
    public void UpdateFrame(BitmapSource? bitmap) => PreviewImage.Source = bitmap;

    /// <summary>Displays plain text content and hides image controls.</summary>
    public void ShowText(string text)
    {
        TextPreview.Text = text;
        TextPreview.Visibility = Visibility.Visible;
        ImageScroll.Visibility = Visibility.Collapsed;
        ZoomToolbar.Visibility = Visibility.Collapsed;
        if (FrameToolbar != null) FrameToolbar.Visibility = Visibility.Collapsed;
        EmptyHint.Visibility = Visibility.Collapsed;
        NoGameHint.Visibility = Visibility.Collapsed;
        PreviewImage.Source = null;
        _hasImage = false;
        _currentSheet = null;
    }

    /// <summary>Displays a centered placeholder message on the stage.</summary>
    public void ShowPlaceholder(string message)
    {
        EmptyHint.Text = message;
        EmptyHint.Visibility = Visibility.Visible;
        NoGameHint.Visibility = Visibility.Collapsed;
        ImageScroll.Visibility = Visibility.Collapsed;
        ZoomToolbar.Visibility = Visibility.Collapsed;
        if (FrameToolbar != null) FrameToolbar.Visibility = Visibility.Collapsed;
        TextPreview.Visibility = Visibility.Collapsed;
        PreviewImage.Source = null;
        _hasImage = false;
        _currentSheet = null;
    }

    /// <summary>Displays the welcome placeholder screen.</summary>
    public void ShowNoGameFolder()
    {
        ShowPlaceholder(string.Empty);
        EmptyHint.Visibility = Visibility.Collapsed;
        NoGameHint.Visibility = Visibility.Visible;
        InfoBar.Visibility = Visibility.Collapsed;
    }

    private void OpenGameFolderLink_Click(object sender, RoutedEventArgs e)
        => OpenGameFolderRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Sets viewport toolbar readout text and displays the info bar.</summary>
    public void SetInfoText(string text)
    {
        InfoTextBlock.Text = text;
        InfoBar.Visibility = string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>Sets background brush for the preview stage.</summary>
    public void SetStageBackdrop(Brush brush) => StageBackdrop.Background = brush;

    /// <summary>Fills the stage behind the image with the neutral tone that stands in for unpainted pixels.</summary>
    public void ShowBlankBackdrop() => StageBackdrop.Background = (Brush)FindResource("StageBlankBrush");

    // ---- Frame toolbar (multi-frame images only) -----------------------------

    private void UpdateFrameToolbar()
    {
        if (_currentSheet != null && _currentSheet.Frames.Count > 1)
        {
            FrameToolbar.Visibility = Visibility.Visible;
            FrameText.Text = $"Frame {_currentFrameIndex + 1} of {_currentSheet.Frames.Count}";
        }
        else
        {
            FrameToolbar.Visibility = Visibility.Collapsed;
        }
    }

    private void PrevFrame_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSheet == null || _currentSheet.Frames.Count <= 1) return;
        _currentFrameIndex--;
        if (_currentFrameIndex < 0) _currentFrameIndex = _currentSheet.Frames.Count - 1;
        PreviewImage.Source = BitmapConverter.ToBitmap(_currentSheet, frameIndex: _currentFrameIndex);
        UpdateFrameToolbar();
    }

    private void NextFrame_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSheet == null || _currentSheet.Frames.Count <= 1) return;
        _currentFrameIndex++;
        if (_currentFrameIndex >= _currentSheet.Frames.Count) _currentFrameIndex = 0;
        PreviewImage.Source = BitmapConverter.ToBitmap(_currentSheet, frameIndex: _currentFrameIndex);
        UpdateFrameToolbar();
    }

    // ---- Zoom / pan ---------------------------------------------------------

    /// <summary>Steps the magnification up.</summary>
    public void ZoomIn() { if (_hasImage) { _isFitMode = false; SetZoom(_zoom * ZoomStep); } }

    /// <summary>Steps the magnification down.</summary>
    public void ZoomOut() { if (_hasImage) { _isFitMode = false; SetZoom(_zoom / ZoomStep); } }

    /// <summary>Shows the image at one game pixel per screen pixel.</summary>
    public void ActualSize() { if (_hasImage) { _isFitMode = false; SetZoom(1.0); } }

    /// <summary>Scales the image to the space available.</summary>
    public void Fit() { if (_hasImage) { _isFitMode = true; FitToWindow(); } }

    private void ZoomIn_Click(object sender, RoutedEventArgs e) => ZoomIn();
    private void ZoomOut_Click(object sender, RoutedEventArgs e) => ZoomOut();
    private void Original_Click(object sender, RoutedEventArgs e) => ActualSize();
    private void Fit_Click(object sender, RoutedEventArgs e) => Fit();

    private void SetZoom(double zoom)
    {
        _zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        PreviewImage.Width = _imageWidth * _zoom;
        PreviewImage.Height = _imageHeight * _zoom;
        ZoomText.Text = $"{Math.Round(_zoom * 100)}%";

        ZoomOrFitChanged?.Invoke(this, EventArgs.Empty);
    }

    private void FitToWindow()
    {
        if (!_hasImage) return;
        double vw = ImageScroll.ViewportWidth, vh = ImageScroll.ViewportHeight;
        if (vw > 0 && vh > 0) SetZoom(Math.Min(vw / _imageWidth, vh / _imageHeight));
    }

    /// <summary>Recalculates fit-to-window scale after layout pass finishes.</summary>
    private void FitWhenLayoutSettles()
    {
        var generation = ++_fitGeneration;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            if (generation != _fitGeneration || !_hasImage || !_isFitMode) return;

            double vw = ImageScroll.ViewportWidth, vh = ImageScroll.ViewportHeight;
            SetZoom(vw > 0 && vh > 0 ? Math.Min(vw / _imageWidth, vh / _imageHeight) : 1.0);
        }));
    }

    private void ImageScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!_hasImage) return;
        e.Handled = true;

        _isFitMode = false;

        var pos = e.GetPosition(ImageScroll);
        double relX = (ImageScroll.HorizontalOffset + pos.X) / (_imageWidth * _zoom);
        double relY = (ImageScroll.VerticalOffset + pos.Y) / (_imageHeight * _zoom);

        SetZoom(_zoom * (e.Delta > 0 ? ZoomStep : 1 / ZoomStep));
        ImageScroll.UpdateLayout();

        ImageScroll.ScrollToHorizontalOffset(relX * (_imageWidth * _zoom) - pos.X);
        ImageScroll.ScrollToVerticalOffset(relY * (_imageHeight * _zoom) - pos.Y);
    }

    private void ImageScroll_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_hasImage || !_isFitMode) return;
        FitToWindow();
        FitWhenLayoutSettles();
    }

    /// <summary>Initializes mouse pan tracking for the preview viewport.</summary>
    private void ImageScroll_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // A tunneling handler, so it runs before the scroll bar is told it was clicked: capturing here
        // would leave the bar inert, with no thumb drag, no paging click and no repeat buttons.
        if (!_hasImage || IsOnScrollBar(e.OriginalSource)) return;
        _panning = true;
        _panStart = e.GetPosition(ImageScroll);
        _panStartH = ImageScroll.HorizontalOffset;
        _panStartV = ImageScroll.VerticalOffset;
        ImageScroll.CaptureMouse();
        ImageScroll.Cursor = Cursors.SizeAll;
    }

    private void ImageScroll_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_panning) return;
        var pos = e.GetPosition(ImageScroll);
        ImageScroll.ScrollToHorizontalOffset(_panStartH - (pos.X - _panStart.X));
        ImageScroll.ScrollToVerticalOffset(_panStartV - (pos.Y - _panStart.Y));
    }

    private void ImageScroll_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_panning) return;
        _panning = false;
        ImageScroll.ReleaseMouseCapture();
        ImageScroll.Cursor = Cursors.Arrow;
    }

    /// <summary>Determines whether the specified visual source belongs to a scrollbar.</summary>
    private static bool IsOnScrollBar(object source)
    {
        for (var node = source as DependencyObject; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (node is ScrollBar) return true;
        }

        return false;
    }
}
