using System.IO;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.App.Views;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.App.ViewModels;

/// <summary>Main application ViewModel managing game lifecycle, collaborators, and commands.</summary>
internal sealed partial class MainViewModel : ObservableObject
{
    private readonly PreviewStage _stage;
    private readonly SessionSettings _settings;
    private readonly PreferencesStore _preferences;

    private ResourceLibrary? _library;
    private string? _gameDir;
    private SkinCatalog? _skins;
    private GameFont? _font;
    private GameData? _data;

    /// <summary>Preview routing coordinator managing active stage presentation.</summary>
    public PreviewRouter Preview { get; }

    /// <summary>Whether a map document is currently active.</summary>
    public bool HasMapDocument => Preview.HasMapDocument;

    /// <summary>Whether a full-screen image or cutscene document is currently active.</summary>
    public bool HasScreenDocument => Preview.HasScreenDocument;

    /// <summary>Whether a composed sprite document is currently active.</summary>
    public bool HasSpriteDocument => Preview.HasSpriteDocument;

    /// <summary>Audio playback ViewModel.</summary>
    public AudioPlayerViewModel Audio { get; }

    /// <summary>Sprite animation playback ViewModel.</summary>
    public AnimationPlayerViewModel Anim { get; }

    /// <summary>Story scene playback ViewModel.</summary>
    public StoryScenePlayerViewModel Story { get; }

    /// <summary>Cutscene playback ViewModel.</summary>
    public CutscenePlayerViewModel Cutscene { get; }

    /// <summary>Layer tree ViewModel.</summary>
    public LayersViewModel Layers { get; }

    /// <summary>Node info readout ViewModel.</summary>
    public InfoViewModel Info { get; }

    /// <summary>Resource navigation tree ViewModel.</summary>
    public NavigationViewModel Nav { get; }

    /// <summary>Export command ViewModel.</summary>
    public ExportViewModel Export => Preview.Export;

    private static readonly TimeSpan TransientStatusDuration = TimeSpan.FromSeconds(5);
    private readonly DispatcherTimer _transientStatusTimer = new();
    private string _persistentStatusText = "Loading…";

    [ObservableProperty] private string _statusText = "Loading…";

    internal MainViewModel(PreviewStage stage, SessionSettings settings, PreferencesStore preferences,
        AudioPlayerViewModel audio, AnimationPlayerViewModel anim, StoryScenePlayerViewModel story,
        CutscenePlayerViewModel cutscene, PanelViewModel panel, NavigationViewModel nav)
    {
        _stage = stage;
        _settings = settings;
        _preferences = preferences;
        Audio = audio;
        Anim = anim;
        Story = story;
        Cutscene = cutscene;
        Layers = panel.Layers;
        Info = panel.Info;
        Nav = nav;

        _transientStatusTimer.Interval = TransientStatusDuration;
        _transientStatusTimer.Tick += (_, _) => ClearTransientStatus();

        Preview = new PreviewRouter(stage, settings, audio, anim, story, cutscene,
            panel.Layers, panel.Info, nav, ShowTransientStatus);
    }

    /// <summary>Displays a temporary status line message that restores after a timeout.</summary>
    private void ShowTransientStatus(string message)
    {
        _transientStatusTimer.Stop();
        StatusText = message;
        _transientStatusTimer.Start();
    }

    /// <summary>Clears any active temporary status message and restores the persistent status line.</summary>
    private void ClearTransientStatus()
    {
        if (!_transientStatusTimer.IsEnabled) return;
        _transientStatusTimer.Stop();
        StatusText = _persistentStatusText;
    }

    /// <summary>Routes the selected navigation tree node to the preview stage.</summary>
    public void HandleResourceSelected(TreeNode node)
    {
        ClearTransientStatus();
        Preview.HandleResourceSelected(node);
    }

    /// <summary>Shows the named resource file in the Raw tree and previews it.</summary>
    /// <returns>The node selected, or null when the tree holds no such file.</returns>
    public TreeNode? ShowRawResource(string resource)
    {
        ClearTransientStatus();
        var from = Nav.SelectedNode;
        int fromTab = Nav.SelectedTreeTab;

        // Previewed from the node rather than left to the tree's selection event, which a node whose row
        // has never been scrolled into view has no container to raise.
        if (Nav.ShowRawResource(resource) is not { } node) return null;

        Preview.HandleResourceSelected(node);

        // A link that lands where the reader already is moves nobody, so it neither offers a way back nor
        // withdraws the one already standing.
        if (from is not null && !ReferenceEquals(from, node))
            Info.Back = new WayBack(from, fromTab, WayBackLabel(from));

        return node;
    }

    /// <summary>Names a node by the path that identifies it, or by its own label where it has none.</summary>
    private static string WayBackLabel(TreeNode node)
        => node.InfoPath is { Count: > 0 } path ? InfoLine.Tight([.. path]) : node.Label;

    /// <summary>Returns to the place the last followed link was followed from.</summary>
    /// <returns>The place returned to, or null when none was on offer.</returns>
    public WayBack? GoBack()
    {
        ClearTransientStatus();
        if (Info.Back is not { } back) return null;

        Nav.Show(back.Node, back.Tab);
        Preview.HandleResourceSelected(back.Node);
        return back;
    }

    /// <summary>Toggles play/pause on whichever media player is currently active.</summary>
    public void ToggleActivePlayback()
    {
        if (Audio.IsVisible)
            Audio.PlayPauseCommand.Execute(null);
        else if (Anim.IsTransportVisible && Anim.CanControlPlayback)
            Anim.PlayPauseCommand.Execute(null);
        else if (Cutscene.HasClip)
            Cutscene.PlayPauseCommand.Execute(null);
        else if (Story.HasScene)
            Story.PlayPauseCommand.Execute(null);
    }

    /// <summary>Toggles audio mute if audio playback is active.</summary>
    public void ToggleAudioMute()
    {
        if (Audio.IsVisible)
            Audio.ToggleMuteCommand.Execute(null);
    }

    /// <summary>Initializes game folder discovery on application startup.</summary>
    public void Initialize()
    {
        if (_preferences.GameFolder is { } remembered
            && GameInstall.TryUseFolder(remembered, out var rememberedDir)
            && TryLoadFrom(rememberedDir))
        {
            return;
        }

        if (GameInstall.TryUseFolder(AppContext.BaseDirectory, out var besideExe) && TryLoadFrom(besideExe))
            return;

        ShowNoInstall();
    }

    /// <summary>Prompts user to select a game folder.</summary>
    [RelayCommand]
    private void OpenGameFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select the Dig It! Game Folder",
            InitialDirectory = _gameDir ?? string.Empty,
        };

        if (dialog.ShowDialog() != true) return;

        if (!GameInstall.TryUseFolder(dialog.FolderName, out var gameDir))
        {
            MessageDialog.Show(System.Windows.Application.Current.MainWindow, "Not a Game Folder",
                $"No Dig It! game archives (.XRS) were found in:\n\n{dialog.FolderName}\n\n" +
                "Select the folder holding the .XRS archives, or one holding a DIGIT folder.");
            return;
        }

        if (!TryLoadFrom(gameDir))
        {
            MessageDialog.Show(System.Windows.Application.Current.MainWindow, "Could Not Read Game Files",
                $"The Dig It! archives in:\n\n{gameDir}\n\n" +
                "could not be read. They may be locked by another program, access may be denied, or they " +
                "may be damaged.");
        }
    }

    /// <summary>Whether a game folder is open: the availability of everything that operates on one.</summary>
    public bool HasGameFolder => _library is not null;

    /// <summary>Closes the active game folder and unloads resources.</summary>
    [RelayCommand(CanExecute = nameof(HasGameFolder))]
    private void CloseGameFolder()
    {
        Unload();
        _preferences.ForgetGameFolder();
        ShowNoInstall();
    }

    /// <summary>Displays no-game-folder placeholder message on the preview stage.</summary>
    private void ShowNoInstall()
    {
        _transientStatusTimer.Stop();
        _persistentStatusText = "No game folder selected";
        StatusText = _persistentStatusText;
        _stage.ShowNoGameFolder();
    }

    /// <summary>Attempts to load game resources and metadata from the specified directory.</summary>
    private bool TryLoadFrom(string gameDir)
    {
        GameInstallContents install;
        try
        {
            install = GameInstall.Open(gameDir);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _preferences.ForgetGameFolderIfCurrent(gameDir);
            return false;
        }

        // Torn down explicitly rather than left to the Attach calls below overwriting what they attached
        // last time: a game folder with an unreadable MAIN.EXE skips most of them, and the outgoing folder's
        // panels would survive into it pointing at archive handles this method has already released.
        Unload();

        _library = install.Library;
        _font = install.Font;
        _data = install.Data;
        _skins = install.Skins;
        _gameDir = gameDir;
        CloseGameFolderCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HasGameFolder));

        _transientStatusTimer.Stop();
        if (_data is { } data)
        {
            Layers.Attach(data.EntityNames);
            Info.Attach(data.Nodes, data.EntityNames, data.WorldMapMusic);
            _persistentStatusText = $"{gameDir}    ·    {_library.Names.Count} resources";
        }
        else
        {
            _persistentStatusText = $"{gameDir}    ·    {_library.Names.Count} resources    ·    " +
                $"{WhyNothingIsNamed(install.MainExe)}. Resources view unavailable, showing Raw files only.";
        }
        StatusText = _persistentStatusText;

        Nav.Attach(_library, _skins, _data);

        Preview.Attach(_library, _font, _data);
        Audio.Attach(_library);
        if (_font is not null) Story.Attach(_library, _font);
        if (_font is not null && _data is not null) Cutscene.Attach(_library, _font, _data.Cutscenes);
        if (_skins is not null) Anim.Attach(_library, _skins);

        var titleFile = _data?.Cutscenes.Pieces[CutscenePiece.DigTitle].FileName ?? "DIGTITLE.ANI";
        if (_library.Names.Any(n => n.Equals(titleFile, StringComparison.OrdinalIgnoreCase)))
            Preview.ShowTitleScreen(titleFile, haveExecutable: _font is not null && _data is not null);

        _preferences.RememberGameFolder(gameDir);

        return true;
    }

    private static string WhyNothingIsNamed(MainExeStatus status) => status switch
    {
        MainExeStatus.Unreadable => "MAIN.EXE could not be read",
        MainExeStatus.DifferentBuild => "MAIN.EXE is from an unrecognized release of the game",
        _ => "MAIN.EXE does not contain the expected game data",
    };

    /// <summary>Unloads active game resources and resets collaborators.</summary>
    private void Unload()
    {
        _transientStatusTimer.Stop();
        Audio.Detach();
        Anim.Detach();
        Story.Detach();
        Cutscene.Detach();
        Nav.Detach();
        Layers.Detach();
        Info.Detach();

        Preview.Detach();

        _library?.Dispose();
        _library = null;
        _gameDir = null;
        _skins = null;
        _font = null;
        _data = null;
        CloseGameFolderCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HasGameFolder));

        _settings.ResetFraming();
    }

    /// <summary>Releases resource library handles on application shutdown.</summary>
    public void Dispose()
    {
        _transientStatusTimer.Stop();
        _library?.Dispose();
    }
}
