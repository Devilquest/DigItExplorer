using System.IO;
using System.Text;
using DigItExplorer.App.Converters;
using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.App.Views;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Export;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Info;

namespace DigItExplorer.App.ViewModels;

/// <summary>Coordinates preview dispatch, player lifecycle, export tracking, and stage updates.</summary>
internal sealed class PreviewRouter
{
    private const string NothingSelectedHint = "Select a resource";

    private readonly PreviewStage _stage;
    private readonly SessionSettings _settings;
    private readonly DocumentPreviews _documents;

    private ResourceLibrary? _library;
    private GameFont? _font;
    private GameData? _data;
    private string? _previewKey;
    private string _exportSubject = "";

    /// <summary>What an exported file is named after, for whichever loader is running.</summary>
    internal string ExportSubject => _exportSubject;

    /// <summary>Whether the preview stage is displaying a full-screen image.</summary>
    internal bool HasScreenImage { get; set; }

    /// <summary>Whether a level or world map document is currently active.</summary>
    internal bool HasMapDocument => _documents.IsMap;

    /// <summary>Whether a full-screen image or cutscene document is currently active.</summary>
    internal bool HasScreenDocument => Story.HasScene || Cutscene.HasClip || HasScreenImage;

    /// <summary>Whether a composed sprite document is currently active.</summary>
    internal bool HasSpriteDocument => _documents.IsSprite;

    private AudioPlayerViewModel Audio { get; }
    private AnimationPlayerViewModel Anim { get; }
    private StoryScenePlayerViewModel Story { get; }
    private CutscenePlayerViewModel Cutscene { get; }
    private LayersViewModel Layers { get; }
    private InfoViewModel Info { get; }
    private NavigationViewModel Nav { get; }

    /// <summary>Export command ViewModel.</summary>
    internal ExportViewModel Export { get; }

    internal PreviewRouter(PreviewStage stage, SessionSettings settings,
        AudioPlayerViewModel audio, AnimationPlayerViewModel anim, StoryScenePlayerViewModel story,
        CutscenePlayerViewModel cutscene, LayersViewModel layers, InfoViewModel info,
        NavigationViewModel nav, Action<string> report)
    {
        _stage = stage;
        _settings = settings;
        Audio = audio;
        Anim = anim;
        Story = story;
        Cutscene = cutscene;
        Layers = layers;
        Info = info;
        Nav = nav;

        _documents = new DocumentPreviews(this, stage, settings, layers, info);

        Export = new ExportViewModel(stage, settings, DescribeVisibleLayers, report);
        Layers.VisibleLayersChanged += OnVisibleLayersChanged;
    }

    /// <summary>Attaches active resource library, font, and game data collaborators.</summary>
    internal void Attach(ResourceLibrary library, GameFont? font, GameData? data)
    {
        _library = library;
        _font = font;
        _data = data;
        _documents.Attach(library, font, data);
    }

    /// <summary>Detaches collaborators and resets preview stage state.</summary>
    internal void Detach()
    {
        _documents.Detach();
        HasScreenImage = false;
        Export.Withdraw();
        _exportSubject = "";
        _stage.ShowPlaceholder(NothingSelectedHint);

        _library = null;
        _font = null;
        _data = null;
        _previewKey = null;
    }

    /// <summary>Navigates to and previews the game title cutscene.</summary>
    internal void ShowTitleScreen(string titleFile, bool haveExecutable)
    {
        _exportSubject = Path.GetFileNameWithoutExtension(titleFile);

        if (haveExecutable)
        {
            if (!Nav.SelectResourcesNode(n => n.Cutscene == CutscenePiece.DigTitle))
                PreviewCutscene(CutscenePiece.DigTitle, [Path.GetFileNameWithoutExtension(titleFile)]);
        }
        else if (!Nav.SelectResourcesNode(n => n.Resource?.Equals(titleFile, StringComparison.OrdinalIgnoreCase) == true))
        {
            PreviewResource(titleFile, []);
        }
    }

    /// <summary>Formats active layer names for export descriptions.</summary>
    private string? DescribeVisibleLayers()
        => _documents.HasCurrent ? $"Layers: {string.Join(" + ", Layers.VisibleLayerNames())}" : null;

    /// <summary>Re-renders composed documents when layer checkboxes change.</summary>
    private void OnVisibleLayersChanged()
    {
        if (_documents.HasCurrent) _documents.RenderCurrent();
    }

    /// <summary>Routes the specified navigation tree node to its appropriate preview loader.</summary>
    internal void HandleResourceSelected(TreeNode node)
    {
        Nav.SetSelectedNode(node);

        var key = node.PreviewKey;
        if (key == _previewKey) return;
        _previewKey = key;

        Info.BeginLoad();
        Export.Withdraw();

        _exportSubject = node.InfoPath is { Count: > 0 } branch ? InfoLine.Of([.. branch]) : node.Label;

        var path = node.InfoPath ?? [];
        if (node.LevelStem is { } stem) _documents.PreviewMap(stem, path);
        else if (node.WorldMap is { } world) _documents.PreviewWorldMap(world, path);
        else if (node.Animation is { } anim) PreviewAnimation(anim);
        else if (node.StoryScene is { } storyWorld) PreviewStoryScene(storyWorld, path);
        else if (node.IntroScene is { } introScene) PreviewIntroStoryScene(introScene, path);
        else if (node.Cutscene is { } piece) PreviewCutscene(piece, path);
        else if (node.EndSequenceScreen is { } screenIndex) _documents.PreviewEndSequenceScreen(screenIndex, path);
        else if (node.Board is { } board) _documents.PreviewMinigameBoard(board, path);
        else if (node.Slab is { } slabRef) _documents.PreviewSlab(slabRef.Screen, slabRef.StopIndex, path);
        else if (node.MainMenu) _documents.PreviewMainMenu(path);
        else if (node.Platform is { } platform) _documents.PreviewPlatform(platform, path);
        else if (node.Drain is { } drainState) _documents.PreviewDrain(drainState, path);
        else if (node.DigSpot is { } digSpot) _documents.PreviewDigSpot(digSpot, path);
        else if (node.ExitSign) _documents.PreviewExitSign(path);
        else if (node.WorldMapSign is { } sign) _documents.PreviewWorldMapSign(sign, path);
        else if (node.Resource is { } name)
            PreviewResource(name, path, node.UseScreensZoom, node.IsRawFile);

        Info.EndLoad();
    }

    // ---- Preview modes ------------------------------------------------------

    /// <summary>Previews a raw file resource or delegated asset format.</summary>
    private void PreviewResource(string name, IReadOnlyList<string> path, bool useScreensZoom = false,
        bool isTheFileItself = false)
    {
        if (_library is null) return;
        _documents.HideLayersPanel();
        HideOtherPlayers();
        HasScreenImage = false;
        var ext = Path.GetExtension(name).ToUpperInvariant();
        var defect = SheetDefect.None;

        try
        {
            if (ResourceLibrary.IsRenderable(name))
            {
                var sheet = SheetImage.Read(_library.Read(name));
                defect = sheet.Defect;

                if (sheet.Frames.Count == 0)
                {
                    _stage.ShowPlaceholder(DamageNote.For(defect) ?? $"{name}: no image data");
                    _stage.SetInfoText(InfoLine.Of([.. path, name]));
                    Info.ShowResource(name, _library, defect, isTheFileItself);
                    return;
                }

                _stage.ShowImage(sheet, useScreensZoom ? _settings.ScreensIsFitMode : true,
                    useScreensZoom ? _settings.ScreensZoom : null);
                HasScreenImage = useScreensZoom;

                if (ExportPolicy.MayExport(name))
                {
                    Export.Offer(sheet.Frames.Count > 1 ? ExportShape.SheetFrames : ExportShape.Still,
                        _exportSubject, sheet.Frames.Count, i => BitmapConverter.ToBitmap(sheet, i));
                }

                string? damaged = defect != SheetDefect.None ? "damaged" : null;
                _stage.SetInfoText(InfoLine.Of([.. path, name, damaged]));
                Info.ShowSheet(name, _library, sheet.Frames.Count, defect, isTheFileItself);
                return;
            }
            else if (ext == ".SMP")
            {
                Audio.PreviewSound(name, path);
                Info.ShowSound(name, _library, _library.Read(name).Length, isTheFileItself);
                return;
            }
            else if (ext == ".DAT")
            {
                var synth = Audio.PreviewMusic(name, path);
                Info.ShowMusic(name, _library, synth, isTheFileItself);
                return;
            }
            else if (ext == ".TXT")
            {
                _stage.ShowText(Encoding.Latin1.GetString(_library.Read(name)));
                _stage.SetInfoText(InfoLine.Of([.. path, name]));
            }
            else
            {
                _stage.ShowPlaceholder($"No preview yet  ·  {ext}  ·  {_library.SizeOf(name):N0} bytes");
                _stage.SetInfoText(InfoLine.Of([.. path, name]));
            }
        }
        catch (Exception ex)
        {
            _stage.ShowPlaceholder($"{name}: {ex.Message}");
            _stage.SetInfoText(InfoLine.Of([.. path, name]));
        }

        Info.ShowResource(name, _library, defect, isTheFileItself);
    }

    // ---- Story scene player --------------------------------------------

    /// <summary>Previews a Traces of Dugette story scene.</summary>
    private void PreviewStoryScene(World world, IReadOnlyList<string> path)
    {
        if (_library is null || _font is null) return;
        _documents.HideLayersPanel();
        HideOtherPlayers(IncomingPlayer.Story);
        HasScreenImage = false;
        Story.PreviewStoryScene(world, path);
        if (Story.HasScene) Export.Offer(ExportShape.Animation, _exportSubject);
    }

    // ---- Cutscene player ------------------------------------------------

    /// <summary>Previews an intro or title cutscene piece.</summary>
    private void PreviewCutscene(CutscenePiece piece, IReadOnlyList<string> path)
    {
        if (_library is null || _font is null || _data is null) return;
        _documents.HideLayersPanel();
        HideOtherPlayers(IncomingPlayer.Cutscene);
        HasScreenImage = false;
        Cutscene.PreviewCutscene(piece, path);
        if (Cutscene.HasClip) Export.Offer(ExportShape.Animation, _exportSubject);
    }

    /// <summary>Previews an intro story scene.</summary>
    private void PreviewIntroStoryScene(IntroScene scene, IReadOnlyList<string> path)
    {
        if (_library is null || _font is null) return;
        _documents.HideLayersPanel();
        HideOtherPlayers(IncomingPlayer.Story);
        HasScreenImage = false;
        Story.PreviewIntroStoryScene(scene, path);
        if (Story.HasScene) Export.Offer(ExportShape.Animation, _exportSubject);
    }

    // ---- Animation player --------------------------------------------

    /// <summary>Previews a sprite character animation.</summary>
    private void PreviewAnimation(AnimationRef animRef)
    {
        if (_library is null) return;
        _documents.HideLayersPanel();
        HideOtherPlayers();
        HasScreenImage = false;
        Anim.PreviewAnimation(animRef);
        if (!Anim.HasAnimation) return;

        var frames = Anim.DistinctFrames;
        Export.Offer(ExportShape.Animation, _exportSubject, frames.Count, i => frames[i], Anim.PlaybackFrames);
    }

    /// <summary>Discriminant indicating which player is about to become active.</summary>
    internal enum IncomingPlayer
    {
        /// <summary>No incoming animated player.</summary>
        None,
        Story,
        Cutscene,
    }

    /// <summary>Stops and hides inactive media players.</summary>
    internal void HideOtherPlayers(IncomingPlayer incoming = IncomingPlayer.None)
    {
        // Deliberately leaves HasScreenImage alone, though most loaders reset it right beside this call:
        // they disagree on whether and where, and folding it in here would change four of their answers.
        Anim.Hide();
        Audio.Hide();
        if (incoming != IncomingPlayer.Story) Story.Hide();
        if (incoming != IncomingPlayer.Cutscene) Cutscene.Hide();
    }
}
