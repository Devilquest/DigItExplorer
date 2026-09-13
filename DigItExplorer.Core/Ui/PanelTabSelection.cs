namespace DigItExplorer.Core.Ui;

/// <summary>Identifies the active tab in the resource side panel.</summary>
public enum PanelTab
{
    /// <summary>Layer visibility checkboxes for the active document.</summary>
    Layers,

    /// <summary>Technical metadata and inspector details.</summary>
    Info,
}

/// <summary>Manages user preference and active display state for side-panel tabs.</summary>
public sealed class PanelTabSelection
{
    /// <summary>Initializes a new instance of PanelTabSelection with an initial chosen tab.</summary>
    /// <param name="chosen">Default selected tab (defaults to PanelTab.Layers).</param>
    public PanelTabSelection(PanelTab chosen = PanelTab.Layers) => Chosen = chosen;

    /// <summary>Tab last selected by the user while both tabs were available.</summary>
    public PanelTab Chosen { get; private set; }

    /// <summary>Whether the current document offers layer visibility options.</summary>
    public bool LayersOffered { get; private set; }

    /// <summary>Whether the current document offers technical metadata.</summary>
    public bool InfoOffered { get; private set; }

    /// <summary>Currently displayed tab based on availability and user preference.</summary>
    public PanelTab Displayed =>
        IsOffered(Chosen) ? Chosen : LayersOffered ? PanelTab.Layers : PanelTab.Info;

    /// <summary>Records which tabs the node now on screen offers.</summary>
    public void Offer(bool layers, bool info)
    {
        LayersOffered = layers;
        InfoOffered = info;
    }

    /// <summary>Records user selection when both tabs are available.</summary>
    public void Choose(PanelTab tab)
    {
        if (!LayersOffered || !InfoOffered) return;
        Chosen = tab;
    }

    private bool IsOffered(PanelTab tab) => tab == PanelTab.Layers ? LayersOffered : InfoOffered;
}
