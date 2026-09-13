using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DigItExplorer.App.Models;

/// <summary>Individual frame step in the animation scrubber timeline.</summary>
/// <param name="thumb">Thumbnail bitmap preview.</param>
/// <param name="frameId">Sprite sheet frame cell index.</param>
/// <param name="index">Sequential step index within the animation.</param>
internal sealed class TimelineStep(BitmapSource thumb, int frameId, int index) : ObservableObject
{
    /// <summary>Thumbnail bitmap preview for this step.</summary>
    public BitmapSource Thumb { get; } = thumb;

    /// <summary>Source sprite sheet frame cell index.</summary>
    public int FrameId { get; } = frameId;

    /// <summary>Step sequence index within the animation.</summary>
    public int Index { get; } = index;

    private bool _isCurrent;

    /// <summary>True while this step is the playhead.</summary>
    public bool IsCurrent
    {
        get => _isCurrent;
        set => SetProperty(ref _isCurrent, value);
    }
}
