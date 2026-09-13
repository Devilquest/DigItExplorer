using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Minigames;

namespace DigItExplorer.App.Models;

/// <summary>Identifies an animation by character set, sheet suffix, and animation index.</summary>
internal sealed record AnimationRef(string Character, string Suffix, int AnimIndex);

/// <summary>Identifies intro cutscene story scenes (INTRODRG or R00).</summary>
internal enum IntroScene { Introdrg, R00 }

/// <summary>Identifies a slab screen topic and resting stop index.</summary>
internal sealed record SlabRef(int Screen, int StopIndex);

/// <summary>Identifies a moving platform by world and sprite cell index.</summary>
internal sealed record PlatformRef(World World, int Cell);

/// <summary>Identifies a dig-spot preview by state.</summary>
internal sealed record DigSpotRef(DigSpotState State);

/// <summary>Identifies a world-map signpost preview by world and signpost type.</summary>
internal sealed record WorldMapSignRef(World World, SignType Type);

/// <summary>The node a link was followed from, offered in the info panel as the way back to it.</summary>
internal sealed record WayBack(TreeNode Node, int Tab, string Label);

/// <summary>Hierarchical node in the navigation resource tree representing a group or selectable leaf.</summary>
internal sealed class TreeNode : ObservableObject
{
    /// <summary>Node label and search filter target text.</summary>
    public string Label { get; init; } = "";

    /// <summary>Total number of child items for counting groups.</summary>
    public int? TotalCount { get; init; }

    /// <summary>Raw resource filename, or null.</summary>
    public string? Resource { get; init; }

    /// <summary>Whether this resource shares persistent screens zoom framing.</summary>
    public bool UseScreensZoom { get; init; }

    /// <summary>Whether this node is the Raw tree's own row for a file, which is the one node a link to
    /// that file would lead back to.</summary>
    public bool IsRawFile { get; init; }

    /// <summary>Level file stem identifier (e.g. LVL000), or null.</summary>
    public string? LevelStem { get; init; }

    /// <summary>World map level select screen, or null.</summary>
    public World? WorldMap { get; init; }

    /// <summary>Animation reference, or null.</summary>
    public AnimationRef? Animation { get; init; }

    /// <summary>Traces of Dugette story scene world, or null.</summary>
    public World? StoryScene { get; init; }

    /// <summary>Intro story scene identifier, or null.</summary>
    public IntroScene? IntroScene { get; init; }

    /// <summary>Cutscene piece identifier, or null.</summary>
    public CutscenePiece? Cutscene { get; init; }

    /// <summary>End sequence screen index (0..9), or null.</summary>
    public int? EndSequenceScreen { get; init; }

    /// <summary>Minigame board type, or null.</summary>
    public MinigameBoard? Board { get; init; }

    /// <summary>Slab screen stop reference, or null.</summary>
    public SlabRef? Slab { get; init; }

    /// <summary>Whether this node represents the main menu screen.</summary>
    public bool MainMenu { get; init; }

    /// <summary>Moving platform cell reference, or null.</summary>
    public PlatformRef? Platform { get; init; }

    /// <summary>Drain state reference, or null.</summary>
    public DrainState? Drain { get; init; }

    /// <summary>Dig spot state reference, or null.</summary>
    public DigSpotRef? DigSpot { get; init; }

    /// <summary>Whether this node represents the exit sign screen.</summary>
    public bool ExitSign { get; init; }

    /// <summary>World-map signpost reference, or null.</summary>
    public WorldMapSignRef? WorldMapSign { get; init; }

    /// <summary>Breadcrumb path hierarchy used for status displays and export naming, which is why an
    /// animation carries one: its own name would not identify the file on its own.</summary>
    public IReadOnlyList<string>? InfoPath { get; init; }

    /// <summary>Complete list of child nodes.</summary>
    public List<TreeNode> Children { get; } = new();

    /// <summary>Filtered collection view of children matching current search query.</summary>
    public ICollectionView VisibleChildren
    {
        get
        {
            if (_visibleChildren is null)
            {
                // Filters the collection rather than hiding the containers: the virtualizing panel has to
                // realize an item to find out it has no size, so hiding would walk the forest per keystroke.
                _visibleChildren = CollectionViewSource.GetDefaultView(Children);
                _visibleChildren.Filter = child => ((TreeNode)child).IsShown;
            }
            return _visibleChildren;
        }
    }

    private ICollectionView? _visibleChildren;

    /// <summary>Whether this node matches the active search filter.</summary>
    internal bool IsShown { get; set; } = true;

    /// <summary>Refreshes the filtered collection view for child nodes.</summary>
    internal void RefreshVisibleChildren() => _visibleChildren?.Refresh();

    /// <summary>Saved expansion state prior to search filtering.</summary>
    internal bool? ExpandedBeforeSearch { get; set; }

    /// <summary>Display caption formatted with active item counts.</summary>
    public string Caption
        => TotalCount is not { } total ? Label
            : _shownCount is { } shown && shown != total ? $"{Label} ({shown} of {total})"
            : $"{Label} ({total})";

    private int? _shownCount;

    /// <summary>Sets the count of visible matching children during search.</summary>
    internal void SetShownCount(int? shown)
    {
        if (_shownCount == shown) return;
        _shownCount = shown;
        OnPropertyChanged(nameof(Caption));
    }

    private bool _isExpanded;
    private bool _isSelected;

    /// <summary>Whether the tree node is currently expanded.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    /// <summary>Whether the tree node is currently selected.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>Unique stable key identifying the preview content represented by this node.</summary>
    public string PreviewKey
    {
        get
        {
            string what =
                LevelStem is { } stem ? $"map:{stem}"
                : WorldMap is { } world ? $"worldmap:{world}"
                : Animation is { } anim ? $"anim:{anim}"
                : StoryScene is { } storyWorld ? $"story:{storyWorld}"
                : IntroScene is { } introScene ? $"intro:{introScene}"
                : Cutscene is { } piece ? $"cutscene:{piece}"
                : EndSequenceScreen is { } screenIndex ? $"endseq:{screenIndex}"
                : Board is { } board ? $"board:{board}"
                : Slab is { } slabRef ? $"slab:{slabRef}"
                : MainMenu ? "mainmenu"
                : Platform is { } platform ? $"platform:{platform}"
                : Drain is { } drainState ? $"drain:{drainState}"
                : DigSpot is { } digSpot ? $"digspot:{digSpot}"
                : ExitSign ? "exitsign"
                : WorldMapSign is { } sign ? $"worldmapsign:{sign}"
                : Resource is { } name ? $"file:{name}:{UseScreensZoom}"
                : "group";

            return $"{what}|{string.Join('/', InfoPath ?? [])}";
        }
    }

    /// <summary>Whether this node is interactive and selectable.</summary>
    public bool IsEnabled => IsSelectable || Children.Count > 0;

    /// <summary>Whether this node represents a selectable resource leaf.</summary>
    public bool IsSelectable => Resource != null || LevelStem != null || WorldMap != null || Animation != null
        || StoryScene != null || IntroScene != null || Cutscene != null || EndSequenceScreen != null
        || Board != null || Slab != null || MainMenu || Platform != null || Drain != null
        || DigSpot != null || ExitSign || WorldMapSign != null;
}
