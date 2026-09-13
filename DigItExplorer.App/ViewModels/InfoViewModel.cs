using CommunityToolkit.Mvvm.ComponentModel;
using DigItExplorer.App.Models;
using DigItExplorer.Core.Audio;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Cutscenes;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Knowledge.Info;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Menu;
using DigItExplorer.Core.Minigames;
using DigItExplorer.Core.Story;

namespace DigItExplorer.App.ViewModels;

/// <summary>ViewModel managing node information sections, metadata, and diagnostic readouts.</summary>
internal sealed partial class InfoViewModel : ObservableObject
{
    [ObservableProperty] private IReadOnlyList<InfoSection>? _sections;

    /// <summary>Standing contextual note for the selected node, or null.</summary>
    [ObservableProperty] private string? _note;

    /// <summary>Asset damage description for the selected node, or null.</summary>
    [ObservableProperty] private string? _damage;

    /// <summary>Whether info sections or notes are available for the selected node.</summary>
    [ObservableProperty] private bool _isAvailable;

    /// <summary>The place the last followed link came from, or null when none is on offer.</summary>
    [ObservableProperty] private WayBack? _back;

    private NodeTableData? _nodes;
    private EntityNames? _entityNames;
    private WorldMapMusic? _worldMapMusic;

    /// <summary>Attaches node table, entity name, and level-select music metadata.</summary>
    public void Attach(NodeTableData nodes, EntityNames entityNames, WorldMapMusic worldMapMusic)
    {
        _nodes = nodes;
        _entityNames = entityNames;
        _worldMapMusic = worldMapMusic;
    }

    /// <summary>Detaches metadata tables and clears info sections.</summary>
    public void Detach()
    {
        _nodes = null;
        _entityNames = null;
        _worldMapMusic = null;
        _loading = false;
        Sections = null;
        Note = null;
        Damage = null;
        Back = null;
        IsAvailable = false;
    }

    /// <summary>Displays info for the specified level map.</summary>
    public void ShowLevel(string stem, MapDocument map, ResourceLibrary library)
        => Show(LevelInfoBuilder.Build(stem, map, library, _nodes, _entityNames));

    /// <summary>Displays info for the specified world map.</summary>
    public void ShowWorldMap(WorldMapDocument map, ResourceLibrary library)
        => Show(WorldMapInfoBuilder.Build(map, library, _nodes, _worldMapMusic));

    /// <summary>Displays info for the specified animation.</summary>
    public void ShowAnimation(CharacterAnimSet set, string suffix, AnimationDef anim,
        ResolvedAnimationSheet? art, SkinCatalog skins, ResourceLibrary library)
        => Show(AnimationInfoBuilder.Build(set, suffix, anim, art, skins, library));

    /// <summary>Displays info for the specified cutscene piece.</summary>
    public void ShowCutscene(CutscenePiece piece, CutsceneData data, CutsceneClip clip, ResourceLibrary library)
        => Show(CutsceneInfoBuilder.Build(piece, data, clip, library));

    /// <summary>Displays info for the specified story scene.</summary>
    public void ShowStoryScene(World? world, string backgroundFile, string textFile, StoryScene scene,
        ResourceLibrary library)
        => Show(StoryInfoBuilder.Build(world is { } w ? _nodes?.WorldName(w) : null, backgroundFile, textFile,
            scene, library));

    /// <summary>Displays info for the specified end sequence screen.</summary>
    public void ShowEndSequence(int screenIndex, EndSequenceData data, ResourceLibrary library)
        => Show(EndSequenceInfoBuilder.Build(screenIndex, data, library));

    /// <summary>Displays info for the specified minigame board.</summary>
    public void ShowMinigameBoard(MinigameBoard board, ResourceLibrary library)
        => Show(MinigameInfoBuilder.Build(board, library));

    /// <summary>Displays info for the specified slab screen stop.</summary>
    public void ShowSlab(int screen, int stopIndex, SlabData slab, ResourceLibrary library)
        => Show(SlabInfoBuilder.Build(screen, stopIndex, slab, library));

    /// <summary>Displays info for the specified main menu document.</summary>
    public void ShowMainMenu(MainMenuDocument menu, ResourceLibrary library)
        => Show(MainMenuInfoBuilder.Build(menu, library, _entityNames));

    /// <summary>Displays info for the specified platform cell.</summary>
    public void ShowPlatform(World world, int cell, int cellCount, (int Width, int Height)? cellSize,
        ResourceLibrary library)
        => Show(PlatformInfoBuilder.Build(world, cell, cellCount, cellSize, library, _nodes));

    /// <summary>Displays info for the specified drain state.</summary>
    public void ShowDrain(DrainState state, (int Width, int Height)? cellSize, ResourceLibrary library)
        => Show(DrainInfoBuilder.Build(state, cellSize, library));

    /// <summary>Displays info for the specified dig spot state.</summary>
    public void ShowDigSpot(DigSpotState state, (int Width, int Height)? cellSize, ResourceLibrary library)
        => Show(DigSpotInfoBuilder.Build(state, cellSize, library));

    /// <summary>Displays info for the specified exit sign.</summary>
    public void ShowExitSign((int Width, int Height)? cellSize, ResourceLibrary library)
        => Show(ExitSignInfoBuilder.Build(cellSize, library, _nodes));

    /// <summary>Displays info for the specified world-map signpost.</summary>
    public void ShowWorldMapSign(World world, SignType type, (int Width, int Height)? cellSize,
        ResourceLibrary library)
        => Show(WorldMapSignInfoBuilder.Build(world, type, cellSize, library, _nodes));

    /// <summary>Displays info for the specified music track.</summary>
    public void ShowMusic(string name, ResourceLibrary library, LdsSynthesizer? synth,
        bool sourceLeadsToItself = false)
        => Show(AudioInfoBuilder.BuildMusic(name, library, synth, sourceLeadsToItself));

    /// <summary>Displays info for the specified sound effect.</summary>
    public void ShowSound(string name, ResourceLibrary library, int byteLength,
        bool sourceLeadsToItself = false)
        => Show(AudioInfoBuilder.BuildSound(name, library, byteLength, sourceLeadsToItself));

    /// <summary>Displays info for the specified raw file resource.</summary>
    public void ShowResource(string name, ResourceLibrary library, SheetDefect defect = SheetDefect.None,
        bool sourceLeadsToItself = false)
        => Show(ResourceInfoBuilder.Build(name, library, defect, sourceLeadsToItself));

    /// <summary>Displays info for a raw graphic sheet file.</summary>
    public void ShowSheet(string name, ResourceLibrary library, int frameCount,
        SheetDefect defect = SheetDefect.None, bool sourceLeadsToItself = false)
        => Show(ResourceInfoBuilder.BuildSheet(name, library, frameCount, defect, sourceLeadsToItself));

    private void Show(NodeInfo info)
    {
        Sections = info.Sections;
        Note = info.Note;
        Damage = info.Damage;
        if (!_loading) IsAvailable = HasContent;
    }

    private bool _loading;

    private bool HasContent => Sections is { Count: > 0 } || Note is not null || Damage is not null;

    /// <summary>Begins info section loading and resets current display state.</summary>
    public void BeginLoad()
    {
        Sections = null;
        Note = null;
        Damage = null;
        Back = null;
        _loading = true;
    }

    /// <summary>Finalizes info section loading and updates tab availability.</summary>
    public void EndLoad()
    {
        _loading = false;
        IsAvailable = HasContent;
    }
}
