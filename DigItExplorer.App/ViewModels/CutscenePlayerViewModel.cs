using System.Globalization;
using System.Text;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigItExplorer.App.Converters;
using DigItExplorer.App.Services;
using DigItExplorer.App.Views;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Cutscenes;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.App.ViewModels;

/// <summary>ViewModel managing intro and title cutscene playback, timing, and frame stepping.</summary>
internal sealed partial class CutscenePlayerViewModel : ObservableObject
{
    /// <summary>Speed picker labels, in the order <see cref="SpeedIndex"/> selects them.</summary>
    public IReadOnlyList<string> SpeedLabels => PlaybackSpeeds.Labels;

    /// <summary>Play/pause button tooltip, phrased for the action the next click performs.</summary>
    public string PlayPauseTooltip => IsPlaying ? "Pause (Space)" : "Play (Space)";

    private readonly PreviewStage _stage;
    private readonly SessionSettings _settings;
    private readonly InfoViewModel _info;
    private readonly TickClock _clock = new();
    private CutsceneClip? _clip;
    private CutsceneData? _data;
    private BitmapPalette? _palette;
    private ResourceLibrary? _library;
    private GameFont? _font;
    private CutsceneData? _cutscenes;

    /// <summary>Delegate determining whether the seek bar is currently captured by mouse input.</summary>
    public Func<bool>? IsSeekBarCaptured { get; set; }

    [ObservableProperty] private BitmapSource? _currentBitmap;
    [ObservableProperty] private bool _hasClip;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseTooltip))]
    private bool _isPlaying;
    [ObservableProperty] private bool _isLoop = true;
    [ObservableProperty] private int _speedIndex = PlaybackSpeeds.DefaultIndex;
    [ObservableProperty] private bool _canStepBack;
    [ObservableProperty] private bool _canStepForward;
    [ObservableProperty] private int _currentIteration;
    [ObservableProperty] private int _totalIterations;
    [ObservableProperty] private string _frameText = "Frame 0 / 0";

    /// <summary>Whether cutscene playback reached the final frame without looping.</summary>
    [ObservableProperty] private bool _hasEnded;

    /// <summary>Current frame duration in milliseconds adjusted for playback speed.</summary>
    public double CurrentFrameMs => _clip is null ? 0 : _clip.FrameMs / PlaybackSpeeds.Multipliers[SpeedIndex];

    public CutscenePlayerViewModel(PreviewStage stage, SessionSettings settings, InfoViewModel info)
    {
        _stage = stage;
        _settings = settings;
        _info = info;
        _clock.Tick += OnTick;
    }

    /// <summary>Attaches resource library, font, and cutscene catalog collaborators.</summary>
    public void Attach(ResourceLibrary library, GameFont font, CutsceneData cutscenes)
    {
        _library = library;
        _font = font;
        _cutscenes = cutscenes;
    }

    /// <summary>Detaches collaborators and stops cutscene playback.</summary>
    public void Detach()
    {
        Hide();
        _library = null;
        _font = null;
        _cutscenes = null;
    }

    /// <summary>Reconstructs and starts playback for the specified cutscene piece.</summary>
    public void PreviewCutscene(CutscenePiece piece, IReadOnlyList<string> path)
    {
        var fileName = _cutscenes!.Pieces[piece].FileName;
        var label = fileName ?? Encoding.Latin1.GetString(_cutscenes.CardText);
        try
        {
            CutsceneClip clip;
            if (fileName is not null)
            {
                var ani = SheetImage.Read(_library!.Read(fileName));
                clip = CutsceneCompositor.Build(piece, ani, _font!, _cutscenes);
            }
            else
            {
                var palBytes = _library!.Read(_cutscenes.CardPaletteFile);
                clip = CutsceneCompositor.BuildCard(_font!, VgaPalette.From6Bit(palBytes.AsSpan(0, 768)), _cutscenes);
            }

            Load(clip, _cutscenes);
            _stage.ShowBitmap(BitmapConverter.ToIndexed8(FrameCodec.Width, FrameCodec.Height,
                CutsceneCompositor.Compose(clip, 0, _cutscenes), clip.Palette), FrameCodec.Width, FrameCodec.Height,
                _settings.ScreensIsFitMode, _settings.ScreensZoom);
            _stage.SetInfoText(InfoLine.Of([.. path, label]));
            _info.ShowCutscene(piece, _cutscenes, clip, _library);
        }
        catch (Exception ex)
        {
            _stage.ShowPlaceholder($"{label}: {ex.Message}");
            _stage.SetInfoText(InfoLine.Of([.. path, label]));
        }
    }

    /// <summary>Loads cutscene clip data and initializes playback timers.</summary>
    public void Load(CutsceneClip clip, CutsceneData data)
    {
        _clip = clip;
        _data = data;
        _palette = BitmapConverter.ToBitmapPalette(clip.Palette);
        HasClip = true;
        HasEnded = false;
        TotalIterations = clip.FrameOrder.Count;
        // Before the first readout: UpdateStepAvailability asks the clock where playback is.
        _clock.Load(clip.FrameMs, clip.FrameOrder.Count);
        RenderTick(0);
        UpdateStepAvailability();
        OnPropertyChanged(nameof(CurrentFrameMs));
        IsPlaying = true;
    }

    /// <summary>Stops playback and clears active cutscene clip state.</summary>
    public void Hide()
    {
        _clock.Unload();
        _clip = null;
        _palette = null;
        HasClip = false;
        IsPlaying = false;
        HasEnded = false;
        CanStepBack = false;
        CanStepForward = false;
        CurrentBitmap = null;
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (_clip is null) return;
        if (IsPlaying)
        {
            Pause();
            return;
        }

        if (HasEnded)
        {
            HasEnded = false;
            _clock.Base = 0;
        }
        IsPlaying = true;
        _clock.Resume();
    }

    [RelayCommand]
    private void StepBack()
    {
        if (_clip is null) return;
        Pause();
        if (_clock.Base > 0) SeekToIteration(_clock.Base - 1);
        else if (IsLoop) SeekToIteration(_clip.FrameOrder.Count - 1);
    }

    [RelayCommand]
    private void StepForward()
    {
        if (_clip is null) return;
        Pause();
        int last = _clip.FrameOrder.Count - 1;
        if (_clock.Base < last) SeekToIteration(_clock.Base + 1);
        else if (IsLoop) SeekToIteration(0);
        else HasEnded = true;
    }

    /// <summary>Seeks playback to the specified frame iteration.</summary>
    public void Seek(int iteration)
    {
        if (_clip is null) return;
        SeekToIteration(Math.Clamp(iteration, 0, _clip.FrameOrder.Count - 1));
        _clock.RestartElapsed(IsPlaying);
    }

    /// <summary>Pauses playback and banks the active iteration tick.</summary>
    private void Pause()
    {
        _clock.Pause();
        IsPlaying = false;
        UpdateStepAvailability();
    }

    private void SeekToIteration(int iteration)
    {
        if (_clip is null) return;
        _clock.Base = iteration;
        HasEnded = false;
        RenderTick(iteration);
        UpdateStepAvailability();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_clip is null) return;
        if (IsSeekBarCaptured?.Invoke() == true) return;

        int tick = _clock.Current();

        if (!IsLoop && tick >= _clip.FrameOrder.Count - 1)
        {
            RenderTick(tick);
            Pause();
            HasEnded = true;
            return;
        }
        RenderTick(tick);
        UpdateStepAvailability();
    }

    /// <summary>Banks the tick reached before the new rule applies, so turning the loop off plays the pass
    /// in progress out to its end instead of ending it at once.</summary>
    partial void OnIsLoopChanged(bool value)
    {
        if (_clip is not null) _clock.Rebase(IsPlaying);
        _clock.Loop = value;
        UpdateStepAvailability();
    }

    partial void OnCurrentBitmapChanged(BitmapSource? value) => _stage.UpdateFrame(value);

    partial void OnSpeedIndexChanged(int value)
    {
        _clock.Speed = PlaybackSpeeds.Multipliers[value];
        OnPropertyChanged(nameof(CurrentFrameMs));
    }

    partial void OnSpeedIndexChanging(int value)
    {
        if (_clip is null) return;
        _clock.Rebase(IsPlaying);
    }

    /// <summary>Updates step command execution availability.</summary>
    private void UpdateStepAvailability()
    {
        if (_clip is null)
        {
            CanStepBack = false;
            CanStepForward = false;
            return;
        }

        int tick = _clock.Current();
        CanStepBack = IsLoop || tick > 0;
        CanStepForward = IsLoop || tick < _clip.FrameOrder.Count - 1;
    }

    private void RenderTick(int iteration)
    {
        if (_clip is null || _palette is null || _data is null) return;
        var frame = CutsceneCompositor.Compose(_clip, iteration, _data);
        CurrentBitmap = BitmapConverter.ToIndexed8(FrameCodec.Width, FrameCodec.Height, frame, _palette);
        CurrentIteration = iteration;
        FrameText = $"Frame {iteration + 1} / {_clip.FrameOrder.Count}";
    }
}
