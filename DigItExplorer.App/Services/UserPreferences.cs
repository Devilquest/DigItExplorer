using System.Text.Json.Serialization;

namespace DigItExplorer.App.Services;

/// <summary>Data contract representing persisted user preferences in settings.json.</summary>
internal sealed class UserPreferences
{
    /// <summary>Settings schema version number.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Last opened game folder path.</summary>
    public string? GameFolder { get; set; }

    /// <summary>Saved window left position coordinate.</summary>
    public double? WindowLeft { get; set; }

    /// <summary>Saved window top position coordinate.</summary>
    public double? WindowTop { get; set; }

    /// <summary>Saved window width dimension.</summary>
    public double? WindowWidth { get; set; }

    /// <summary>Saved window height dimension.</summary>
    public double? WindowHeight { get; set; }

    /// <summary>Whether the window was maximized.</summary>
    public bool WindowIsMaximized { get; set; }

    /// <summary>Persisted audio volume slider level.</summary>
    public double AudioVolume { get; set; } = 1.0;

    /// <summary>Persisted last audible volume level for unmute.</summary>
    public double LastAudibleVolume { get; set; } = 1.0;

    /// <summary>Persisted export magnification scale factor.</summary>
    public int ExportScale { get; set; } = 1;

    /// <summary>Persisted export format name.</summary>
    public string? ExportFormat { get; set; }

    /// <summary>Persisted chosen side-panel tab name.</summary>
    public string? ChosenPanelTab { get; set; }

    /// <summary>Persisted side-panel visibility state.</summary>
    public bool IsPanelVisible { get; set; } = true;
}

/// <summary>Source-generated JSON serializer context for UserPreferences.</summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UserPreferences))]
internal sealed partial class UserPreferencesJsonContext : JsonSerializerContext;
