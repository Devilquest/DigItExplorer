using System.Linq;
using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Menu;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.ViewModels.Layers;

/// <summary>Manages the layer tree and visibility settings for the main menu screen (LVL900).</summary>
internal sealed class MainMenuLayerForest : LayerForest
{
    // Its own scope, so its Terrain and Collision stay a separate choice from a level map's.
    private const string ScopeName = "MainMenu";

    private readonly EntityNames? _entityNames;
    private readonly bool _hasBackground;
    private readonly bool _hasCollision;
    private readonly IReadOnlyList<DlfRecord> _entities;
    private readonly HashSet<(byte Category, ushort? Type)> _keys;
    private readonly (bool MainSign, bool Play, bool Setup, bool Intro) _signs;

    internal MainMenuLayerForest(SessionSettings settings, EntityNames? entityNames, bool hasBackground,
        bool hasCollision, IReadOnlyList<DlfRecord> entities, bool hasMainSign, bool hasSetup, bool hasPlay,
        bool hasIntro) : base(settings)
    {
        _entityNames = entityNames;
        _hasBackground = hasBackground;
        _hasCollision = hasCollision;
        _entities = entities;
        _keys = entities.Select(EntityCategories.KeyOf).ToHashSet();
        _signs = (hasMainSign, hasPlay, hasSetup, hasIntro);
    }

    protected override string Scope => ScopeName;

    protected override IEnumerable<(byte Category, ushort? Type)> EntityKeys => _keys;

    public override bool SameShapeAs(LayerForest other)
        => other is MainMenuLayerForest menu
            && menu._hasBackground == _hasBackground
            && menu._hasCollision == _hasCollision
            && menu._keys.SetEquals(_keys)
            && menu._signs == _signs;

    public override List<LayerNode> Build()
    {
        var baseChildren = new List<LayerNode>();
        if (_hasBackground)
            baseChildren.Add(LayerNode.Leaf("Background", MapLayer.MainMenuBackground));
        baseChildren.Add(LayerNode.Leaf("Terrain", MapLayer.Terrain));
        if (_hasCollision)
            baseChildren.Add(LayerNode.Leaf("Collision", MapLayer.Collision).Keyed(LayerShortcutGroup.Collision));
        var roots = new List<LayerNode> { BaseGroup(baseChildren) };

        var enemyChildren = new List<(string SortKey, LayerNode Node)>();
        foreach (var catGroup in _entities.GroupBy(e => e.Category))
        {
            string label = EntityCategories.LabelOf(catGroup.Key, _entityNames);
            enemyChildren.Add((label, LayerNode.CategoryLeaf($"{label} ({catGroup.Count()})", catGroup.Key)));
        }
        if (enemyChildren.Count > 0)
        {
            var ordered = enemyChildren.OrderBy(c => c.SortKey, StringComparer.OrdinalIgnoreCase).Select(c => c.Node).ToArray();
            roots.Add(LayerNode.Group("Enemies", ordered).Keyed(LayerShortcutGroup.Enemies));
        }

        if (_signs.MainSign)
        {
            var mainChild = LayerNode.Leaf("Dig It!", MapLayer.MainMenuMainSign);
            roots.Add(LayerNode.Group("Main", mainChild));
        }

        var subMenuChildren = new List<LayerNode>();
        if (_signs.Setup)
            subMenuChildren.Add(LayerNode.Leaf("Setup", MapLayer.MainMenuSetup));
        if (_signs.Play)
            subMenuChildren.Add(LayerNode.Leaf("Play", MapLayer.MainMenuPlay));
        if (_signs.Intro)
            subMenuChildren.Add(LayerNode.Leaf("Intro", MapLayer.MainMenuIntro));
        if (subMenuChildren.Count > 0)
        {
            var subMenuGroup = LayerNode.Group("Sub-menu", subMenuChildren.ToArray());
            subMenuGroup.IsExclusiveGroup = true;
            roots.Add(subMenuGroup);
        }

        return roots;
    }

    /// <summary>Extracts main menu render options from session settings and active entity keys.</summary>
    internal static MainMenuRenderOptions RenderOptionsFrom(SessionSettings s,
        IReadOnlySet<(byte Category, ushort? Type)> visibleKeys)
        => new(Visible(s, MapLayer.MainMenuBackground), Visible(s, MapLayer.Terrain),
            Visible(s, MapLayer.Collision), visibleKeys, Visible(s, MapLayer.MainMenuMainSign),
            Visible(s, MapLayer.MainMenuSetup), Visible(s, MapLayer.MainMenuPlay),
            Visible(s, MapLayer.MainMenuIntro));

    private static bool Visible(SessionSettings s, MapLayer layer) => s.IsShown(layer);
}
