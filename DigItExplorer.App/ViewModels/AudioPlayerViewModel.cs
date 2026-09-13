using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigItExplorer.App.Audio;
using DigItExplorer.App.Services;
using DigItExplorer.App.Views;
using DigItExplorer.Core.Audio;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;
using NAudio.Wave;

namespace DigItExplorer.App.ViewModels;

/// <summary>ViewModel managing sound effects and synthesized music playback, seeking, and volume.</summary>
internal sealed partial class AudioPlayerViewModel : ObservableObject
{
    private readonly SessionSettings _settings;
    private readonly PreviewStage _stage;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(50) };

    private WaveOut? _waveOut;
    private WaveStream? _audioStream;
    private CancellationTokenSource? _musicRenderCts;
    private LdsSynthesizer? _musicSynth;
    private ResourceLibrary? _library;
    private bool _isMusicSelected;
    private bool _soundLoop;
    // Null until the loop toggle is used on music: until then each track's own loop flag sets it.
    private bool? _musicLoop;
    private bool _applyingLoopDefault;

    /// <summary>Delegate determining whether the seek bar is currently captured by mouse input.</summary>
    public Func<bool>? IsSeekBarCaptured { get; set; }

    /// <summary>Play/pause button tooltip, phrased for the action the next click performs.</summary>
    public string PlayPauseTooltip => IsPlaying ? "Pause (Space)" : "Play (Space)";

    /// <summary>Mute button tooltip, phrased for the action the next click performs.</summary>
    public string MuteTooltip => IsMuted ? "Unmute (M)" : "Mute (M)";

    [ObservableProperty] private bool _isVisible;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseTooltip))]
    private bool _isPlaying;
    [ObservableProperty] private bool _isLoop;
    [ObservableProperty] private double _volume;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MuteTooltip))]
    private bool _isMuted;
    [ObservableProperty] private double _seekMax = 1;
    [ObservableProperty] private long _seekPosition;
    [ObservableProperty] private string _timeText = "0.00 / 0.00 s";

    internal AudioPlayerViewModel(SessionSettings settings, PreviewStage stage)
    {
        _settings = settings;
        _stage = stage;
        _volume = settings.AudioVolume;
        _isMuted = settings.AudioVolume == 0;
        _timer.Tick += OnTimerTick;
    }

    /// <summary>Attaches the active resource library.</summary>
    public void Attach(ResourceLibrary library) => _library = library;

    /// <summary>Detaches the resource library and stops audio playback.</summary>
    public void Detach()
    {
        Hide();
        _library = null;
    }

    /// <summary>Loads and plays an uncompressed SMP sound effect.</summary>
    public void PreviewSound(string name, IReadOnlyList<string> path)
    {
        var raw = _library!.Read(name);
        _stage.ShowPlaceholder($"♪  {name}");
        _stage.SetInfoText(InfoLine.Of([.. path, name]));
        _isMusicSelected = false;
        ApplyLoopDefault(_soundLoop);
        Play(new LoopStream(new RawSourceWaveStream(new MemoryStream(SmpAudio.ToPcm16(raw)),
            new WaveFormat(SmpAudio.SampleRate, SmpAudio.BitsPerSample, SmpAudio.Channels))));
    }

    /// <summary>Initializes synthesized LDS music playback and starts background prefill.</summary>
    public LdsSynthesizer? PreviewMusic(string name, IReadOnlyList<string> path)
    {
        var data = _library!.Read(name);
        var synth = LdsSynthesizer.TryCreate(data);
        if (synth is null)
        {
            _stage.ShowPlaceholder($"No preview yet  ·  .DAT  ·  {_library.SizeOf(name):N0} bytes");
            _stage.SetInfoText(InfoLine.Of([.. path, name]));
            return null;
        }

        _isMusicSelected = true;
        ApplyLoopDefault(_musicLoop ?? synth.Loops);
        var stream = new SynthWaveStream(synth);
        _stage.ShowPlaceholder($"♪  {name}");
        _stage.SetInfoText(InfoLine.Of([.. path, name]));
        StartMusicPrefill(synth);
        Play(stream);
        return synth;
    }

    /// <summary>Asynchronously prefills synthesized PCM samples into the synthesizer buffer.</summary>
    public void StartMusicPrefill(LdsSynthesizer synth)
    {
        _musicSynth = synth;
        _musicRenderCts = new CancellationTokenSource();
        var ct = _musicRenderCts.Token;
        _ = Task.Run(() =>
        {
            try
            {
                const int chunk = LdsRenderer.DefaultSampleRate / 4;
                for (long s = chunk; ; s += chunk)
                {
                    synth.EnsureRendered(Math.Min(s, synth.TotalSamples), ct);
                    if (s >= synth.TotalSamples) break;
                    Thread.Sleep(1);
                }
            }
            catch (OperationCanceledException) { }
        }, CancellationToken.None);
    }

    /// <summary>Formats duration in seconds as standard time text.</summary>
    public static string FormatTime(double seconds)
        => seconds >= 60
            ? $"{(int)(seconds / 60)}:{(int)(seconds % 60):D2} min"
            : string.Create(CultureInfo.InvariantCulture, $"{seconds:0.00} s");

    /// <summary>Initializes wave audio output and starts playback.</summary>
    public void Play(WaveStream source)
    {
        _audioStream = source;
        ((ILoopingStream)source).EnableLooping = IsLoop;
        // Two buffers of this length queue 150 ms of audio, so a seek is heard promptly.
        _waveOut = new WaveOut { BufferMilliseconds = 75 };
        _waveOut.Init(_audioStream);
        _waveOut.Volume = (float)Volume;
        _waveOut.PlaybackStopped += WaveOut_PlaybackStopped;

        SeekMax = _audioStream.Length;
        RefreshSeekBar();
        IsVisible = true;

        StartPlayback();
    }

    /// <summary>Stops playback, releases the audio output device, and hides transport controls.</summary>
    public void Hide()
    {
        _musicRenderCts?.Cancel();
        _musicRenderCts = null;
        _musicSynth?.Abort();
        _musicSynth = null;
        _timer.Stop();
        IsPlaying = false;
        if (_waveOut is not null)
        {
            var waveOut = _waveOut;
            _waveOut = null;
            waveOut.PlaybackStopped -= WaveOut_PlaybackStopped;
            waveOut.Dispose();
        }
        _audioStream?.Dispose();
        _audioStream = null;
        IsVisible = false;
    }

    private void StartPlayback()
    {
        if (_waveOut is null || _audioStream is null) return;
        if (_audioStream.Position >= _audioStream.Length) _audioStream.Position = 0;
        _waveOut.Play();
        IsPlaying = true;
        _timer.Start();
    }

    private void PausePlayback()
    {
        if (_waveOut is null) return;
        _waveOut.Pause();
        PausePlaybackUi();
    }

    private void PausePlaybackUi()
    {
        IsPlaying = false;
        _timer.Stop();
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (IsPlaying) PausePlayback();
        else StartPlayback();
    }

    [RelayCommand]
    private void Stop()
    {
        if (_waveOut is null || _audioStream is null) return;
        _waveOut.Stop();
        _audioStream.Position = 0;
        PausePlaybackUi();
        RefreshSeekBar();
    }

    [RelayCommand]
    private void ToggleMute()
        => Volume = Volume > 0 ? 0 : _settings.LastAudibleVolume;

    partial void OnVolumeChanged(double value)
    {
        _settings.AudioVolume = value;
        if (value > 0) _settings.LastAudibleVolume = value;
        if (_waveOut is not null) _waveOut.Volume = (float)value;
        IsMuted = value == 0;
    }

    private void ApplyLoopDefault(bool value)
    {
        _applyingLoopDefault = true;
        IsLoop = value;
        _applyingLoopDefault = false;
    }

    partial void OnIsLoopChanged(bool value)
    {
        if (_audioStream is ILoopingStream loop) loop.EnableLooping = value;
        if (_applyingLoopDefault) return;
        if (_isMusicSelected) _musicLoop = value;
        else _soundLoop = value;
    }

    private void WaveOut_PlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (_waveOut is null || _audioStream is null) return;
        PausePlaybackUi();
        _audioStream.Position = 0;
        RefreshSeekBar();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (IsSeekBarCaptured?.Invoke() == true) return;
        RefreshSeekBar();
    }

    private void RefreshSeekBar()
    {
        if (_audioStream is null) return;
        SeekPosition = Math.Min(_audioStream.Position, _audioStream.Length);
        UpdateTimeText();
    }

    private void UpdateTimeText()
    {
        if (_audioStream is null) return;
        double bytesPerSecond = _audioStream.WaveFormat.AverageBytesPerSecond;
        double current = _audioStream.Position / bytesPerSecond;
        double total = _audioStream.Length / bytesPerSecond;

        bool buffering = _musicSynth is not null && IsPlaying
                         && _audioStream is SynthWaveStream sws
                         && sws.PhysicalSamplePosition > _musicSynth.RenderedSamples;

        string text;
        if (total >= 60)
        {
            text = $"{FormatClock(current)} / {FormatClock(total)}";
        }
        else
        {
            string pad = new('0', ((int)total).ToString(CultureInfo.InvariantCulture).Length);
            text = string.Create(CultureInfo.InvariantCulture, $"{current.ToString($"{pad}.00", CultureInfo.InvariantCulture)} / {total:0.00} s");
        }
        TimeText = buffering ? $"{text} ⏳" : text;

        static string FormatClock(double s) => $"{(int)s / 60}:{(int)s % 60:D2}";
    }

    /// <summary>Seeks playback stream to the specified byte position.</summary>
    public void Seek(long position)
    {
        if (_audioStream is null) return;
        position -= position % _audioStream.WaveFormat.BlockAlign;
        _audioStream.Position = Math.Min(position, _audioStream.Length);
        UpdateTimeText();
    }
}
