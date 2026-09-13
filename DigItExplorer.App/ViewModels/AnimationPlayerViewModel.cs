using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigItExplorer.App.Converters;
using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.App.Views;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Export;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.ViewModels;

/// <summary>ViewModel managing sprite animation playback, transport controls, and frame stepping.</summary>
internal sealed partial class AnimationPlayerViewModel : ObservableObject
{
    private sealed record LoadedAnimation(string CharacterKey, string Character, string SkinLabel,
        string AnimLabel, AnimationDef Anim, IReadOnlyList<BitmapSource> StepBitmaps,
        IReadOnlyList<TimelineStep> Steps, double BaseMs, AnimationFrames Playback);

    private Action? _regenerate;

    private const double HoldMs = 600; // End-of-cycle hold duration in milliseconds.

    /// <summary>Speed picker labels, in the order <see cref="SpeedIndex"/> selects them.</summary>
    public IReadOnlyList<string> SpeedLabels => PlaybackSpeeds.Labels;

    /// <summary>Play/pause button tooltip, phrased for the action the next click performs.</summary>
    public string PlayPauseTooltip => IsPlaying ? "Pause (Space)" : "Play (Space)";

    private readonly PreviewStage _stage;
    private readonly SessionSettings _settings;
    private readonly InfoViewModel _info;
    private readonly Brush _checkerBrush;
    private readonly DispatcherTimer _timer = new(DispatcherPriority.Render);
    private LoadedAnimation? _current;
    private ResourceLibrary? _library;
    private SkinCatalog? _skins;

    [ObservableProperty] private bool _hasAnimation;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseTooltip))]
    private bool _isPlaying;
    [ObservableProperty] private bool _isLoop = true;
    [ObservableProperty] private bool _showCheckerboardBackdrop;
    [ObservableProperty] private int _speedIndex = PlaybackSpeeds.DefaultIndex;
    [ObservableProperty] private bool _isTransportVisible;
    [ObservableProperty] private bool _isTimelineVisible;
    [ObservableProperty] private bool _canControlPlayback;
    [ObservableProperty] private bool _canRegenerate;
    [ObservableProperty] private IReadOnlyList<TimelineStep>? _steps;
    [ObservableProperty] private BitmapSource? _currentBitmap;
    [ObservableProperty] private int _currentStepIndex;
    [ObservableProperty] private string _frameText = string.Empty;

    public AnimationPlayerViewModel(PreviewStage stage, SessionSettings settings, InfoViewModel info, Brush checkerBrush)
    {
        _stage = stage;
        _settings = settings;
        _info = info;
        _checkerBrush = checkerBrush;
        _timer.Tick += OnTick;
    }

    /// <summary>Attaches the active resource library and skin catalog.</summary>
    public void Attach(ResourceLibrary library, SkinCatalog skins)
    {
        _library = library;
        _skins = skins;
    }

    /// <summary>Detaches collaborators and stops active animation playback.</summary>
    public void Detach()
    {
        Hide();
        _library = null;
        _skins = null;
    }

    /// <summary>Loads and starts playback for the specified animation reference.</summary>
    public void PreviewAnimation(AnimationRef animRef)
    {
        if (_library is null) return;

        try
        {
            var set = AnimationTables.All[animRef.Character];
            var art = AnimationSheetResolver.Resolve(set, animRef.Suffix, _library.TryRead, out var failure);
            if (art is null)
            {
                _stage.ShowPlaceholder($"{animRef.Character}: " + (failure == AnimationSheetFailure.NoGrid
                    ? "no sheet grid known" : "sheet or palette missing in this copy of the game"));
                return;
            }
            var (pages, palette, rects, grid) = (art.Pages, art.Palette, art.Rects, art.Grid);

            var anim = set.Anims[animRef.AnimIndex];
            bool isLiveSim = anim.Name == "idle_wait_sim";
            if (isLiveSim)
                anim = anim with { Frames = DugWaitSimulator.Walk(Random.Shared, pairs: 12) };

            var (stageW, stageH) = SkinCatalog.CellSize(rects, grid, anim);

            var underlay = anim.Underlay is { } u
                ? SkinCatalog.ResolveCell(rects, grid, pages, u.Frame)
                : (SpriteCell?)null;
            var byFrame = new Dictionary<int, BitmapSource>();
            var placedByFrame = new Dictionary<int, PlacedFrame>();
            foreach (var f in anim.Frames.Distinct())
            {
                var cell = SkinCatalog.ResolveCell(rects, grid, pages, f);
                if (underlay is { } under)
                    cell = cell.Over(under, anim.Underlay!.Value.OffsetX, anim.Underlay.Value.OffsetY,
                        set.TransparentIndices);

                var placed = FrameCanvas.Place(cell, stageW, stageH, set.TransparentIndices);
                placedByFrame[f] = placed;
                byFrame[f] = BitmapConverter.PlacedToBitmap(placed, palette);
            }

            var stepBitmaps = anim.Frames.Select(f => byFrame[f]).ToList();
            var steps = anim.Frames.Select((f, i) => new TimelineStep(byFrame[f], f, i)).ToList();
            double baseMs = AnimationTables.DefaultFrameMs * anim.TicksPerStep;

            var skinLabel = _skins!.TryGetCached(set.Name)?.FirstOrDefault(s => s.Suffix == animRef.Suffix)?.Label
                ?? (set.FixedSheet is not null ? _skins.LocationLabel(set)
                    : set.SheetLetter is not null
                        ? (PlayerCostumes.All.First(c => c.Code == animRef.Suffix[0]).World is World w2
                            ? _skins.SkinLabel(set, w2, unused: false) : "Unused costume")
                    : _skins.SkinLabel(set, animRef.Suffix, unused: false));
            var canvas = placedByFrame[anim.Frames[0]];
            var playback = new AnimationFrames(
                [.. anim.Frames.Select(f => placedByFrame[f].Indices)],
                canvas.Width, canvas.Height, palette, baseMs, anim.Mode == AnimMode.Loop);

            Load(set.Name, _skins.CharacterLabel(set), skinLabel, _skins.AnimLabel(set, anim), anim,
                stepBitmaps, steps, baseMs, playback,
                regenerate: isLiveSim ? () => PreviewAnimation(animRef) : null);
            _stage.ShowBitmap(stepBitmaps[0], stageW, stageH, _settings.AnimationIsFitMode, _settings.AnimationZoom);
            ApplyStageBackdrop();
            _info.ShowAnimation(set, animRef.Suffix, anim, art, _skins, _library);
        }
        catch (Exception ex)
        {
            Hide();
            _stage.ShowPlaceholder($"{animRef.Character}: {ex.Message}");
            _stage.SetInfoText(animRef.Character);
        }
    }

    /// <summary>Loads decoded animation frames, timeline steps, and playback settings.</summary>
    public void Load(string characterKey, string character, string skinLabel, string animLabel,
        AnimationDef anim, IReadOnlyList<BitmapSource> stepBitmaps, IReadOnlyList<TimelineStep> steps,
        double baseMs, AnimationFrames playback, Action? regenerate = null)
    {
        _current = new LoadedAnimation(characterKey, character, skinLabel, animLabel, anim, stepBitmaps, steps,
            baseMs, playback);
        _regenerate = regenerate;
        CanRegenerate = regenerate is not null;
        HasAnimation = true;
        CurrentStepIndex = 0;
        Steps = steps;
        steps[0].IsCurrent = true;
        CurrentBitmap = stepBitmaps[0];
        IsTimelineVisible = true;
        IsTransportVisible = true;

        _stage.SetInfoText(AnimationStatusLine.Identity(character, skinLabel, animLabel));
        UpdateInfo();
        if (AnimationStatusLine.IsStill(anim.Mode, anim.Frames.Count))
        {
            CanControlPlayback = false;
            return;
        }

        CanControlPlayback = true;
        StartPlayback();
    }

    /// <summary>List of unique frame bitmaps utilized by the current animation.</summary>
    public IReadOnlyList<BitmapSource> DistinctFrames =>
        Steps is null ? []
        : [.. Steps.GroupBy(s => s.FrameId).Select(g => g.First().Thumb)];

    /// <summary>Animation frame index sequence and timing for playback reproduction.</summary>
    public AnimationFrames? PlaybackFrames => _current is { } current && current.Playback.Steps.Count > 1
        ? current.Playback
        : null;

    /// <summary>Stops playback, hides transport controls, and clears stage backdrops.</summary>
    public void Hide()
    {
        _timer.Stop();
        IsPlaying = false;
        _current = null;
        _regenerate = null;
        CanRegenerate = false;
        HasAnimation = false;
        IsTransportVisible = false;
        IsTimelineVisible = false;
        CanControlPlayback = false;
        Steps = null;
        CurrentBitmap = null;
        FrameText = string.Empty;
        _stage.SetStageBackdrop(Brushes.Transparent);
    }

    [RelayCommand]
    private void Regenerate() => _regenerate?.Invoke();

    private void StartPlayback()
    {
        IsPlaying = true;
        ScheduleNextTick();
    }

    private void PausePlayback()
    {
        IsPlaying = false;
        _timer.Stop();
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (_current is null) return;
        if (IsPlaying)
        {
            PausePlayback();
        }
        else
        {
            // Play from current frame, wrapping to start if on the final frame.
            if (CurrentStepIndex == _current.Steps.Count - 1)
                SetStep(0);
            StartPlayback();
        }
    }

    [RelayCommand]
    private void StepBack()
    {
        if (_current is null) return;
        PausePlayback();
        int n = _current.Steps.Count;
        SetStep((CurrentStepIndex - 1 + n) % n);
    }

    [RelayCommand]
    private void StepForward()
    {
        if (_current is null) return;
        PausePlayback();
        SetStep((CurrentStepIndex + 1) % _current.Steps.Count);
    }

    /// <summary>Jumps playback position to the specified timeline step index.</summary>
    public void JumpToStep(int index)
    {
        if (_current is null) return;
        SetStep(index);
    }

    /// <summary>Schedules the timer interval for the current animation step.</summary>
    private void ScheduleNextTick()
    {
        if (_current is null) return;
        bool holding = _current.Anim.Mode != AnimMode.Loop && !_current.Anim.Seamless
            && CurrentStepIndex == _current.Steps.Count - 1;
        double ms = holding ? HoldMs : _current.BaseMs / PlaybackSpeeds.Multipliers[SpeedIndex];
        _timer.Interval = TimeSpan.FromMilliseconds(ms);
        _timer.Start();
    }

    /// <summary>Calculates the active millisecond duration per step.</summary>
    private double CurrentStepMs() => _current is null ? 0 : _current.BaseMs / PlaybackSpeeds.Multipliers[SpeedIndex];

    private void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        if (_current is null || !IsPlaying) return;

        int next = CurrentStepIndex + 1;
        if (next >= _current.Steps.Count)
        {
            if (!IsLoop)
            {
                PausePlayback();
                return;
            }
            next = 0;
        }
        SetStep(next);
        ScheduleNextTick();
    }

    /// <summary>Updates the active timeline step index and stage bitmap.</summary>
    private void SetStep(int index)
    {
        if (_current is null) return;
        _current.Steps[CurrentStepIndex].IsCurrent = false;
        CurrentStepIndex = index;
        _current.Steps[index].IsCurrent = true;
        CurrentBitmap = _current.StepBitmaps[index];
        UpdateInfo();
    }

    private void UpdateInfo()
    {
        if (_current is null) return;
        var anim = _current.Anim;
        FrameText = AnimationStatusLine.StepProgress(anim.Mode, anim.Frames, CurrentStepIndex, CurrentStepMs(), anim.StepsAreState);
    }

    partial void OnSpeedIndexChanged(int value)
    {
        if (_current is null) return;
        UpdateInfo();
        if (IsPlaying)
        {
            _timer.Stop();
            ScheduleNextTick();
        }
    }

    /// <summary>Updates preview stage backdrop between transparent and checkerboard patterns.</summary>
    private void ApplyStageBackdrop()
        => _stage.SetStageBackdrop(HasAnimation && ShowCheckerboardBackdrop ? _checkerBrush : Brushes.Transparent);

    partial void OnCurrentBitmapChanged(BitmapSource? value) => _stage.UpdateFrame(value);

    partial void OnShowCheckerboardBackdropChanged(bool value) => ApplyStageBackdrop();
}
