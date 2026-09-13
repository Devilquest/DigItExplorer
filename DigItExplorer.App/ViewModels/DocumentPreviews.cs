using DigItExplorer.App.Converters;
using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.App.ViewModels.Documents;
using DigItExplorer.App.ViewModels.Layers;
using DigItExplorer.App.Views;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Menu;
using DigItExplorer.Core.Minigames;
using DigItExplorer.Core.Slabs;

namespace DigItExplorer.App.ViewModels;

/// <summary>Coordinates loading, framing, and layer integration for composed preview documents.</summary>
internal sealed class DocumentPreviews
{
    private readonly PreviewRouter _owner;
    private readonly PreviewStage _stage;
    private readonly SessionSettings _settings;
    private readonly LayersViewModel _layers;
    private readonly InfoViewModel _info;
    private readonly ComposedDocuments _documents = new();

    private ResourceLibrary? _library;
    private GameFont? _font;
    private GameData? _data;
    private PreviewContext? _context;

    internal DocumentPreviews(PreviewRouter owner, PreviewStage stage, SessionSettings settings,
        LayersViewModel layers, InfoViewModel info)
    {
        _owner = owner;
        _stage = stage;
        _settings = settings;
        _layers = layers;
        _info = info;
    }

    /// <summary>Whether a composed document is currently loaded.</summary>
    internal bool HasCurrent => _documents.HasCurrent;

    /// <summary>Whether a level or world map document is currently loaded.</summary>
    internal bool IsMap => _documents.CurrentBucket == ZoomBucket.Map;

    /// <summary>Whether a composed moving platform sprite document is currently loaded.</summary>
    internal bool IsSprite => _documents.CurrentBucket == ZoomBucket.AnimationPlayer;

    /// <summary>Attaches the active resource library, font, and game data collaborators.</summary>
    internal void Attach(ResourceLibrary library, GameFont? font, GameData? data)
    {
        _library = library;
        _font = font;
        _data = data;
        _context = new PreviewContext(_stage, _settings, library, font, data, _layers, _info);
    }

    /// <summary>Detaches collaborators and clears composed document state.</summary>
    internal void Detach()
    {
        Clear();
        _library = null;
        _font = null;
        _data = null;
        _context = null;
    }

    /// <summary>Clears the active composed document.</summary>
    internal void Clear() => _documents.Clear();

    /// <summary>Clears the active document and hides the layers panel.</summary>
    internal void HideLayersPanel()
    {
        Clear();
        _layers.Clear();
    }

    /// <summary>Renders the active document and updates export offers.</summary>
    internal void RenderCurrent()
    {
        bool rendered = _documents.Render();
        // Assigned after the render and never before: the render raises ZoomOrFitChanged synchronously,
        // and the window's handler reads this while it still holds the previous preview's value.
        _owner.HasScreenImage = rendered && _documents.CurrentBucket == ZoomBucket.ScreensUi;

        if (rendered) _owner.Export.Offer(ExportShape.Still, _owner.ExportSubject);
        else _owner.Export.Withdraw();
    }

    /// <summary>Loads and displays level map terrain, collisions, and entity layers.</summary>
    internal void PreviewMap(string stem, IReadOnlyList<string> path)
    {
        if (_library is null || _context is null) return;
        Clear();
        _owner.HideOtherPlayers();
        _owner.HasScreenImage = false;

        var infoPath = path.Count > 0 ? path : [stem];

        try
        {
            var map = MapDocument.Load(stem, _library.TryRead);
            if (map is null)
            {
                ShowUnavailable($"{stem}: no terrain (F.MPF) in this copy of the game", infoPath);
                return;
            }

            var still = new MapStill(_context, map, infoPath);
            still.CaptureFraming();
            _layers.Ensure(map.Collision is not null, map.Entities, map.ColliderKeys,
                map.HasBonusDestinations, map.HasExitDestinations);
            _documents.Load(still);
            RenderCurrent();
            _info.ShowLevel(stem, map, _library);
        }
        catch (Exception ex)
        {
            ShowUnavailable($"{stem}: {ex.Message}", infoPath);
        }
    }

    /// <summary>Loads and displays world map terrain, paths, and sign layers.</summary>
    internal void PreviewWorldMap(World world, IReadOnlyList<string> path)
    {
        if (_library is null || _data is null || _context is null) return;
        Clear();
        _owner.HideOtherPlayers();
        _owner.HasScreenImage = false;

        var infoPath = path.Count > 0 ? path : [_data.Nodes.WorldName(world), "World Map"];

        var prefix = GameKnowledge.WorldMapPrefix(world);
        try
        {
            var doc = WorldMapDocument.Load(world, _library.TryRead, _data.Nodes);
            if (doc is null)
            {
                ShowUnavailable($"{prefix}: no world map (LD.MPF) in this copy of the game", infoPath);
                return;
            }

            var still = new WorldMapStill(_context, doc, infoPath);
            still.CaptureFraming();
            _layers.EnsureWorldMap(doc.Sky is not null, doc.Background is not null,
                doc.Path is not null, doc.SignTypesPresent());
            _documents.Load(still);
            RenderCurrent();
            _info.ShowWorldMap(doc, _library);
        }
        catch (Exception ex)
        {
            ShowUnavailable($"{prefix}: {ex.Message}", infoPath);
        }
    }

    /// <summary>Loads and displays an end sequence credit screen.</summary>
    internal void PreviewEndSequenceScreen(int screenIndex, IReadOnlyList<string> path)
    {
        if (_library is null || _font is null || _data is null || _context is null) return;
        Clear();
        _owner.HideOtherPlayers();

        try
        {
            var sheet = SheetImage.Read(_library.Read(_data.EndSequence.FileName));
            _layers.EnsureEndSequence();
            _documents.Load(new EndSequenceStill(_context, sheet, screenIndex, path));
            RenderCurrent();
            _info.ShowEndSequence(screenIndex, _data.EndSequence, _library);
        }
        catch (Exception ex)
        {
            ShowUnavailable($"ENDSEQ.MPF: {ex.Message}", [.. path, _data.EndSequence.FileName]);
        }
    }

    /// <summary>Loads and displays a bonus minigame board.</summary>
    internal void PreviewMinigameBoard(MinigameBoard board, IReadOnlyList<string> path)
    {
        if (_library is null || _context is null) return;
        Clear();
        _owner.HideOtherPlayers();

        _layers.EnsureMinigameBoard(board);
        _documents.Load(new MinigameBoardStill(_context, board, path));
        RenderCurrent();
        _info.ShowMinigameBoard(board, _library);
    }

    /// <summary>Loads and displays an Instructions or Credits slab resting stop.</summary>
    internal void PreviewSlab(int screen, int stopIndex, IReadOnlyList<string> path)
    {
        if (_library is null || _font is null || _data is null || _context is null) return;
        Clear();
        _owner.HideOtherPlayers();

        try
        {
            _layers.EnsureSlab(_library.TryRead(_data.Slab.ParallaxFile) is not null);
            _documents.Load(new SlabStill(_context, screen, stopIndex, path));
            RenderCurrent();
            _info.ShowSlab(screen, stopIndex, _data.Slab, _library);
        }
        catch (Exception ex)
        {
            ShowUnavailable($"SLB{screen:00}: {ex.Message}", path);
        }
    }

    /// <summary>Loads and displays the main menu screen (LVL900).</summary>
    internal void PreviewMainMenu(IReadOnlyList<string> path)
    {
        if (_library is null || _context is null) return;
        Clear();
        _owner.HideOtherPlayers();

        try
        {
            var doc = MainMenuDocument.Load(_library.TryRead);
            if (doc is null)
            {
                ShowUnavailable("LVL900: missing terrain, DLF header, or back parallax in this copy of the game", path);
                return;
            }

            _layers.EnsureMainMenu(hasBackground: true, doc.Map.Collision is not null, doc.Map.Entities,
                doc.Signs.ContainsKey("MENU01.MPF"), doc.Signs.ContainsKey("MENU02.MPF"),
                doc.Signs.ContainsKey("MENU03.MPF"), doc.Signs.ContainsKey("MENU04.MPF"));
            _documents.Load(new MainMenuStill(_context, doc, path));
            RenderCurrent();
            _info.ShowMainMenu(doc, _library);
        }
        catch (Exception ex)
        {
            ShowUnavailable($"LVL900: {ex.Message}", path);
        }
    }

    /// <summary>Loads and displays a moving platform sprite cell and collider footprint.</summary>
    internal void PreviewPlatform(PlatformRef platform, IReadOnlyList<string> path)
    {
        if (_library is null || _context is null) return;
        Clear();
        _owner.HasScreenImage = false;
        _owner.HideOtherPlayers();

        var still = new PlatformStill(_context, platform, path);
        _layers.EnsureSprite();
        _documents.Load(still);
        RenderCurrent();
        _info.ShowPlatform(platform.World, platform.Cell,
            PlatformCompositor.CellCount(_library.TryRead, platform.World), still.CellSize, _library);
        still.RefreshInfoText();
    }

    /// <summary>Loads and displays a water world bonus drain state.</summary>
    internal void PreviewDrain(DrainState state, IReadOnlyList<string> path)
    {
        if (_library is null || _context is null) return;
        Clear();
        _owner.HasScreenImage = false;
        _owner.HideOtherPlayers();

        var still = new DrainStill(_context, state, path);
        _layers.EnsureSprite();
        _documents.Load(still);
        RenderCurrent();
        _info.ShowDrain(state, still.CellSize, _library);
        still.RefreshInfoText();
    }

    /// <summary>Loads and displays a dig spot sprite marker.</summary>
    internal void PreviewDigSpot(DigSpotRef digSpot, IReadOnlyList<string> path)
    {
        if (_library is null || _context is null) return;
        HideLayersPanel();
        _owner.HasScreenImage = false;
        _owner.HideOtherPlayers();

        var still = new DigSpotStill(_context, digSpot.State, path);
        _documents.Load(still);
        RenderCurrent();
        _info.ShowDigSpot(digSpot.State, still.CellSize, _library);
    }

    /// <summary>Loads and displays the water world exit sign.</summary>
    internal void PreviewExitSign(IReadOnlyList<string> path)
    {
        if (_library is null || _context is null) return;
        HideLayersPanel();
        _owner.HasScreenImage = false;
        _owner.HideOtherPlayers();

        var still = new ExitSignStill(_context, path);
        _documents.Load(still);
        RenderCurrent();
        _info.ShowExitSign(still.CellSize, _library);
    }

    /// <summary>Loads and displays a world-map signpost still.</summary>
    internal void PreviewWorldMapSign(WorldMapSignRef sign, IReadOnlyList<string> path)
    {
        if (_library is null || _context is null) return;
        HideLayersPanel();
        _owner.HasScreenImage = false;
        _owner.HideOtherPlayers();

        var still = new WorldMapSignStill(_context, sign.World, sign.Type, path);
        _documents.Load(still);
        RenderCurrent();
        _info.ShowWorldMapSign(sign.World, sign.Type, still.CellSize, _library);
    }

    /// <summary>Displays an unavailable document placeholder message on the stage.</summary>
    private void ShowUnavailable(string message, IReadOnlyList<string> infoPath)
    {
        HideLayersPanel();
        _owner.HasScreenImage = false;
        _stage.ShowPlaceholder(message);
        _stage.SetInfoText(InfoLine.Of([.. infoPath]));
    }
}
