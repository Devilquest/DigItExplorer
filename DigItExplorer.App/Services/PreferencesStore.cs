using System.IO;
using System.Text.Json;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.Services;

/// <summary>Persists and retrieves user preferences to and from disk in JSON format.</summary>
internal sealed class PreferencesStore
{
    // Environment.SpecialFolder.ApplicationData does not change while the process runs, so resolving it
    // once here is equivalent to asking every time.
    private static readonly string? FilePath = ResolveFilePath();

    private readonly UserPreferences _current;

    private PreferencesStore(UserPreferences current) => _current = current;

    private static string? ResolveFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return string.IsNullOrEmpty(appData) ? null : Path.Combine(appData, "DigItExplorer", "settings.json");
    }

    /// <summary>Loads preferences from disk, or returns default preferences on error.</summary>
    public static PreferencesStore Load()
    {
        if (FilePath is not null)
        {
            try
            {
                var json = File.ReadAllText(FilePath);
                var loaded = JsonSerializer.Deserialize(json, UserPreferencesJsonContext.Default.UserPreferences);
                if (loaded is { Version: 1 }) return new PreferencesStore(loaded);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // Falls through to defaults below.
            }
        }

        return new PreferencesStore(new UserPreferences());
    }

    /// <summary>Last opened game folder path, or null.</summary>
    public string? GameFolder => _current.GameFolder;

    /// <summary>Saves the specified game folder path as the active location.</summary>
    public void RememberGameFolder(string gameDir)
    {
        if (string.Equals(_current.GameFolder, gameDir, StringComparison.OrdinalIgnoreCase)) return;
        _current.GameFolder = gameDir;
        SaveToFile();
    }

    /// <summary>Clears the saved game folder path unconditionally.</summary>
    public void ForgetGameFolder()
    {
        if (_current.GameFolder is null) return;
        _current.GameFolder = null;
        SaveToFile();
    }

    /// <summary>Clears the saved game folder path if it matches the specified folder.</summary>
    public void ForgetGameFolderIfCurrent(string gameDir)
    {
        if (!string.Equals(_current.GameFolder, gameDir, StringComparison.OrdinalIgnoreCase)) return;
        _current.GameFolder = null;
        SaveToFile();
    }

    /// <summary>Saved window bounds from the previous session, or null.</summary>
    public WindowRect? SavedWindowRect =>
        _current is { WindowLeft: { } left, WindowTop: { } top, WindowWidth: { } width, WindowHeight: { } height }
            ? new WindowRect(left, top, width, height)
            : null;

    /// <summary>Whether the window was maximized in the previous session.</summary>
    public bool WindowIsMaximized => _current.WindowIsMaximized;

    /// <summary>Saves window bounds and maximized state for the next session.</summary>
    public void SaveWindowGeometry(WindowRect rect, bool isMaximized)
    {
        _current.WindowLeft = rect.Left;
        _current.WindowTop = rect.Top;
        _current.WindowWidth = rect.Width;
        _current.WindowHeight = rect.Height;
        _current.WindowIsMaximized = isMaximized;
        SaveToFile();
    }

    /// <summary>Persisted audio playback volume level.</summary>
    public double AudioVolume => _current.AudioVolume;

    /// <summary>Persisted last audible volume level for unmute operations.</summary>
    public double LastAudibleVolume => _current.LastAudibleVolume;

    /// <summary>Persisted export magnification scale factor.</summary>
    public int ExportScale => _current.ExportScale;

    /// <summary>Persisted export format identifier name.</summary>
    public string? ExportFormatName => _current.ExportFormat;

    /// <summary>Persisted active side-panel tab name.</summary>
    public string? ChosenPanelTabName => _current.ChosenPanelTab;

    /// <summary>Persisted side-panel visibility state.</summary>
    public bool IsPanelVisible => _current.IsPanelVisible;

    /// <summary>Persists user preference settings for the next session.</summary>
    public void SavePersonPreferences(double audioVolume, double lastAudibleVolume, int exportScale,
        string exportFormatName, string chosenPanelTabName, bool isPanelVisible)
    {
        _current.AudioVolume = audioVolume;
        _current.LastAudibleVolume = lastAudibleVolume;
        _current.ExportScale = exportScale;
        _current.ExportFormat = exportFormatName;
        _current.ChosenPanelTab = chosenPanelTabName;
        _current.IsPanelVisible = isPanelVisible;
        SaveToFile();
    }

    /// <summary>Serializes and writes preferences to disk.</summary>
    private void SaveToFile()
    {
        if (FilePath is null) return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

            var json = JsonSerializer.Serialize(_current, UserPreferencesJsonContext.Default.UserPreferences);

            // Written beside the file and moved over it, so a crash between the two leaves the old one whole.
            var tempPath = FilePath + ".new";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, FilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Swallowed, per this class's summary.
        }
    }
}
