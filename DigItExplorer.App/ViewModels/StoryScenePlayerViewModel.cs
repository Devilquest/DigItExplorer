using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigItExplorer.App.Converters;
using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.App.Views;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Story;

namespace DigItExplorer.App.ViewModels;

/// <summary>ViewModel managing Traces of Dugette story scene playback, timing, and line stepping.</summary>
internal sealed partial class StoryScenePlayerViewModel : ObservableObject
{
    /// <summary>Speed picker labels, in the order <see cref="SpeedIndex"/> selects them.</summary>
    public IReadOnlyList<string> SpeedLabels => PlaybackSpeeds.Labels;

    /// <summary>Play/pause button tooltip, phrased for the action the next click performs.</summary>
    public string PlayPauseTooltip => IsPlaying ? "Pause (Space)" : "Play (Space)";

    private readonly PreviewStage _stage;
    private readonly SessionSettings _settings;
    private readonly InfoViewModel _info;
    private readonly TickClock _clock = new();
    private StoryScene? _scene;
    private BitmapPalette? _palette;
    private ResourceLibrary? _library;
    private GameFont? _font;
    private string _storyInfoText = "";

    [ObservableProperty] private BitmapSource? _currentBitmap;
    [ObservableProperty] private bool _hasScene;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseTooltip))]
    private bool _isPlaying;
    [ObservableProperty] private bool _isLoop = true;
    [ObservableProperty] private int _speedIndex = PlaybackSpeeds.DefaultIndex;
    [ObservableProperty] private bool _canStepBack;
    [ObservableProperty] private bool _canStepForward;

    /// <summary>Whether playback reached the final frame without looping.</summary>
    [ObservableProperty] private bool _hasEnded;

    public StoryScenePlayerViewModel(PreviewStage stage, SessionSettings settings, InfoViewModel info)
    {
        _stage = stage;
        _settings = settings;
        _info = info;
        _clock.Tick += OnTick;
    }

    /// <summary>Attaches resource library and font collaborators.</summary>
    public void Attach(ResourceLibrary library, GameFont font)
    {
        _library = library;
        _font = font;
    }

    /// <summary>Detaches collaborators and stops playback.</summary>
    public void Detach()
    {
        Hide();
        _library = null;
        _font = null;
    }

    /// <summary>Reconstructs and starts playback for a world story scene.</summary>
    public void PreviewStoryScene(World world, IReadOnlyList<string> path)
    {
        var scrnName = $"R0{(int)world + 1}_SCRN.SPF";
        var stryName = $"R0{(int)world + 1}_STRY.TXT";
        try
        {
            var background = SheetImage.Read(_library!.Read(scrnName));
            var storyTxt = _library.Read(stryName);
            var scene = StoryReconstructor.Build(_font!, background, storyTxt, world);

            Load(scene);
            _stage.ShowBitmap(BitmapConverter.ToIndexed8(FrameCodec.Width, FrameCodec.Height, scene.Background, scene.Palette),
                FrameCodec.Width, FrameCodec.Height, _settings.ScreensIsFitMode, _settings.ScreensZoom);
            _storyInfoText = InfoLine.Of([.. path]);
            _stage.SetInfoText(_storyInfoText);
            _info.ShowStoryScene(world, scrnName, stryName, scene, _library);
        }
        catch (Exception ex)
        {
            _stage.ShowPlaceholder($"{scrnName}: {ex.Message}");
            _stage.SetInfoText(InfoLine.Of([.. path, scrnName]));
        }
    }

    /// <summary>Reconstructs and starts playback for an intro story scene.</summary>
    public void PreviewIntroStoryScene(IntroScene scene, IReadOnlyList<string> path)
    {
        var (scrnName, stryName, mode) = scene == IntroScene.Introdrg
            ? ("INTRODRG.SPF", "INTRODRG.TXT", 1)
            : ("R00_SCRN.SPF", "R00_STRY.TXT", 0);
        try
        {
            var background = SheetImage.Read(_library!.Read(scrnName));
            var storyTxt = _library.Read(stryName);
            var sceneData = StoryReconstructor.Build(_font!, background, storyTxt, mode);

            Load(sceneData);
            _stage.ShowBitmap(BitmapConverter.ToIndexed8(FrameCodec.Width, FrameCodec.Height, sceneData.Background, sceneData.Palette),
                FrameCodec.Width, FrameCodec.Height, _settings.ScreensIsFitMode, _settings.ScreensZoom);
            _storyInfoText = InfoLine.Of([.. path]);
            _stage.SetInfoText(_storyInfoText);
            _info.ShowStoryScene(null, scrnName, stryName, sceneData, _library);
        }
        catch (Exception ex)
        {
            _stage.ShowPlaceholder($"{scrnName}: {ex.Message}");
            _stage.SetInfoText(InfoLine.Of([.. path, scrnName]));
        }
    }

    /// <summary>Loads story scene data and initializes playback timers.</summary>
    public void Load(StoryScene scene)
    {
        _scene = scene;
        _palette = BitmapConverter.ToBitmapPalette(scene.Palette);
        HasScene = true;
        HasEnded = false;
        // Before the first readout: UpdateStepAvailability asks the clock where playback is.
        _clock.Load(StoryReconstructor.FrameMs, scene.ScrollOffsets.Length);
        RenderTick(0);
        UpdateStepAvailability();
        IsPlaying = true;
    }

    /// <summary>Stops playback and clears active story scene state.</summary>
    public void Hide()
    {
        _clock.Unload();
        _scene = null;
        _palette = null;
        HasScene = false;
        IsPlaying = false;
        HasEnded = false;
        CanStepBack = false;
        CanStepForward = false;
        CurrentBitmap = null;
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (_scene is null) return;
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
        if (_scene is null) return;
        Pause();
        if (PreviousPlateauStart(_clock.Base) is int previous)
        {
            SeekToTick(previous);
            return;
        }

        if (IsLoop) SeekToTick(LastPlateauStart());
    }

    [RelayCommand]
    private void StepForward()
    {
        if (_scene is null) return;
        Pause();
        int target = NextPlateauStart(_clock.Base) ?? (IsLoop ? 0 : _scene.ScrollOffsets.Length - 1);
        SeekToTick(target);

        if (!IsLoop && NextPlateauStart(target) is null) HasEnded = true;
    }

    /// <summary>Pauses playback and banks the active tick position.</summary>
    private void Pause()
    {
        _clock.Pause();
        IsPlaying = false;
        UpdateStepAvailability();
    }

    private void SeekToTick(int tick)
    {
        if (_scene is null) return;
        _clock.Base = tick;
        HasEnded = false;
        RenderTick(tick);
        UpdateStepAvailability();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_scene is null) return;
        int tick = _clock.Current();

        if (!IsLoop && tick >= _scene.ScrollOffsets.Length - 1)
        {
            RenderTick(tick);
            Pause();
            HasEnded = true;
            return;
        }
        RenderTick(tick);
        UpdateStepAvailability();
    }

    /// <summary>Banks the tick reached before the new rule applies, so turning repeat off plays the pass
    /// in progress out to its end instead of ending it at once.</summary>
    partial void OnIsLoopChanged(bool value)
    {
        if (_scene is not null) _clock.Rebase(IsPlaying);
        _clock.Loop = value;
        UpdateStepAvailability();
    }

    partial void OnCurrentBitmapChanged(BitmapSource? value) => _stage.UpdateFrame(value);

    partial void OnSpeedIndexChanging(int value)
    {
        if (_scene is null) return;
        _clock.Rebase(IsPlaying);
    }

    partial void OnSpeedIndexChanged(int value) => _clock.Speed = PlaybackSpeeds.Multipliers[value];

    /// <summary>Determines whether the specified tick marks a line plateau landing point.</summary>
    private static bool IsPlateauStart(int[] offsets, int i)
        => i > 0 && i < offsets.Length - 1 && offsets[i] != offsets[i - 1] && offsets[i] == offsets[i + 1];

    /// <summary>Finds the next tick index where a text line settles into view.</summary>
    private int? NextPlateauStart(int fromTick)
    {
        var offsets = _scene!.ScrollOffsets;
        for (int i = fromTick + 1; i < offsets.Length; i++)
            if (IsPlateauStart(offsets, i)) return i;
        return null;
    }

    /// <summary>Finds the previous tick index where a text line settled into view.</summary>
    private int? PreviousPlateauStart(int fromTick)
    {
        var offsets = _scene!.ScrollOffsets;
        int currentValue = offsets[fromTick];
        int i = fromTick;
        while (i > 0 && offsets[i - 1] == currentValue) i--;
        if (i == 0) return null;

        i--; // Walk back to previous landing tick.
        while (i > 0 && !IsPlateauStart(offsets, i)) i--;
        return i;
    }

    /// <summary>Finds the tick index where the final text line settles into view.</summary>
    private int LastPlateauStart()
    {
        var offsets = _scene!.ScrollOffsets;
        for (int i = offsets.Length - 2; i > 0; i--)
            if (IsPlateauStart(offsets, i)) return i;
        return 0;
    }

    /// <summary>Updates step command execution availability.</summary>
    private void UpdateStepAvailability()
    {
        if (_scene is null)
        {
            CanStepBack = false;
            CanStepForward = false;
            return;
        }

        int tick = _clock.Current();
        CanStepBack = IsLoop || PreviousPlateauStart(tick) is not null;
        CanStepForward = IsLoop || NextPlateauStart(tick) is not null;
    }

    private void RenderTick(int tickIndex)
    {
        if (_scene is null || _palette is null) return;
        var frame = StoryReconstructor.Compose(_scene, tickIndex);
        CurrentBitmap = BitmapConverter.ToIndexed8(FrameCodec.Width, FrameCodec.Height, frame, _palette);
    }
}
