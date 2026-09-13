using DigItExplorer.App.Models;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.Services;

/// <summary>Session-lifetime sticky settings and layer visibility toggles.</summary>
internal sealed class SessionSettings
{
    /// <summary>Initializes a new instance of SessionSettings with persisted preference values.</summary>
    internal SessionSettings(double audioVolume, double lastAudibleVolume, int exportScale,
        ExportFormat exportFormat, PanelTab chosenPanelTab)
    {
        ResetFraming();
        AudioVolume = audioVolume;
        LastAudibleVolume = lastAudibleVolume;
        ExportScale = exportScale;
        ExportFormat = exportFormat;
        PanelTabs = new PanelTabSelection(chosenPanelTab);
    }

    /// <summary>Resets viewport zoom and fit modes to initial session defaults.</summary>
    public void ResetFraming()
    {
        AnimationZoom = 4.5;
        AnimationIsFitMode = false;
        ScreensZoom = 1.0;
        ScreensIsFitMode = true;
        MapIsOneToOne = false;
    }

    /// <summary>Active side-panel tab selection state.</summary>
    public PanelTabSelection PanelTabs { get; }

    /// <summary>Layer visibility carried between documents, following the most recent thing the user said
    /// about a layer or about a group holding it.</summary>
    public StickyLayers Layers { get; } = new();

    /// <summary>Which structural layers the open document draws, which is what the panel shows rather than
    /// what is remembered for documents to come.</summary>
    public Dictionary<MapLayer, bool> ShownLayers { get; } = new();

    /// <summary>Whether the open document draws the specified layer, false for one it does not contain.</summary>
    /// <param name="layer">The structural layer being asked about.</param>
    public bool IsShown(MapLayer layer) => ShownLayers.TryGetValue(layer, out bool shown) && shown;

    /// <summary>Selected export magnification scale factor.</summary>
    public int ExportScale { get; set; }

    /// <summary>Selected export format.</summary>
    public ExportFormat ExportFormat { get; set; }

    /// <summary>Current audio playback volume level.</summary>
    public double AudioVolume { get; set; }

    /// <summary>Last audible volume level for unmute operations.</summary>
    public double LastAudibleVolume { get; set; }

    /// <summary>Sticky zoom level for animation and moving-platform previews.</summary>
    public double AnimationZoom { get; set; }

    /// <summary>Sticky fit mode for animation and moving-platform previews.</summary>
    public bool AnimationIsFitMode { get; set; }

    /// <summary>Sticky zoom level for screens, cutscenes, and still image previews.</summary>
    public double ScreensZoom { get; set; }

    /// <summary>Sticky fit mode for screens, cutscenes, and still image previews.</summary>
    public bool ScreensIsFitMode { get; set; }

    /// <summary>Whether 1:1 pixel scaling is enabled for map previews.</summary>
    public bool MapIsOneToOne { get; set; }
}
