using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DigItExplorer.App.Services;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.ViewModels;

/// <summary>ViewModel managing side panel tab selection, visibility, and sync.</summary>
internal sealed partial class PanelViewModel : ObservableObject
{
    private readonly PanelTabSelection _tabs;

    /// <summary>The Layers tab's content.</summary>
    public LayersViewModel Layers { get; }

    /// <summary>The Info tab's content.</summary>
    public InfoViewModel Info { get; }

    /// <summary>User preference determining side panel visibility.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PanelToggleTooltip))]
    private bool _isPanelVisible;

    /// <summary>Whether any side panel tab has content available for display.</summary>
    [ObservableProperty] private bool _isAvailable;

    /// <summary>Whether the Layers tab is available for the current document.</summary>
    [ObservableProperty] private bool _isLayersTabAvailable;

    /// <summary>Active tab index selection.</summary>
    [ObservableProperty] private int _selectedTabIndex;

    /// <summary>Panel toggle tooltip, phrased for the action the next click performs.</summary>
    public string PanelToggleTooltip => IsPanelVisible ? "Hide Side Panel (P)" : "Show Side Panel (P)";

    internal PanelViewModel(SessionSettings settings, LayersViewModel layers, InfoViewModel info, bool isPanelVisible)
    {
        _tabs = settings.PanelTabs;
        Layers = layers;
        Info = info;
        IsPanelVisible = isPanelVisible;
        Layers.PropertyChanged += OnTabContentChanged;
        Info.PropertyChanged += OnTabContentChanged;
        Sync();
    }

    /// <summary>Shows or hides the panel, as its toolbar button does, and does nothing while it is unavailable.</summary>
    public void TogglePanel()
    {
        if (!IsAvailable) return;
        IsPanelVisible = !IsPanelVisible;
    }

    private void OnTabContentChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LayersViewModel.IsAvailable)) Sync();
    }

    /// <summary>Synchronizes tab availability and active tab selection.</summary>
    private void Sync()
    {
        _tabs.Offer(Layers.IsAvailable, Info.IsAvailable);
        IsLayersTabAvailable = Layers.IsAvailable;
        IsAvailable = Layers.IsAvailable || Info.IsAvailable;
        SelectedTabIndex = (int)_tabs.Displayed;
    }

    partial void OnSelectedTabIndexChanged(int value) => _tabs.Choose((PanelTab)value);
}
