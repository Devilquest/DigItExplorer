using DigItExplorer.App.Models;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.App.Services.ResourceTree;

/// <summary>Constructs resource tree branches for level objects, hazards, mechanisms, and UI elements.</summary>
internal static class ObjectAndUiBranches
{
    /// <summary>Builds the objects and hazards branch covering decor, hazards, mechanisms, and markers.</summary>
    public static TreeNode? ObjectsAndHazards(SkinCatalog skins, ResourceLibrary library, GameData data)
    {
        var root = new TreeNode { Label = "Objects & Hazards", IsExpanded = true };

        var decor = new TreeNode { Label = "Decor" };
        CharacterNodes.Add(decor, skins, [AnimationTables.Plant, AnimationTables.Bubble], collapseCharacter: false);
        if (decor.Children.Count > 0) root.Children.Add(decor);

        var hazards = new TreeNode { Label = "Hazards" };
        CharacterNodes.Add(hazards, skins,
            [AnimationTables.FallingRock, AnimationTables.Snowball, AnimationTables.NirpEgg], collapseCharacter: false);
        if (hazards.Children.Count > 0) root.Children.Add(hazards);

        var mechanisms = new TreeNode { Label = "Mechanisms" };
        AddPlatformNodes(mechanisms, library, data);
        AddDrainNodes(mechanisms, library, data);
        if (mechanisms.Children.Count > 0) root.Children.Add(mechanisms);

        var markers = new TreeNode { Label = "Markers" };
        AddDigSpotNodes(markers, library);
        AddExitSignNode(markers, library);
        AddWorldMapSignNodes(markers, library, data);
        if (markers.Children.Count > 0) root.Children.Add(markers);

        return root.Children.Count > 0 ? root : null;
    }

    /// <summary>Appends moving platform mechanism subtrees grouped by platform kind.</summary>
    private static void AddPlatformNodes(TreeNode parent, ResourceLibrary library, GameData data)
    {
        World[] worlds = [World.Caves, World.Water, World.Snow, World.Underworld];
        // A world that ships no platform sheet reports no cells: the water world never does.
        var cellCounts = worlds.ToDictionary(w => w, w => PlatformCompositor.CellCount(library.TryRead, w));

        for (int cell = 0; cell < cellCounts.Values.Max(); cell++)
        {
            string kindLabel = EntityCategories.SubtypeNameOf(PlatformCategory, (ushort)cell) ?? $"Type {cell}";
            var kindNode = new TreeNode { Label = kindLabel };
            foreach (var world in worlds)
            {
                if (cell >= cellCounts[world]) continue;

                string worldName = data.Nodes.WorldName(world);
                kindNode.Children.Add(new TreeNode
                {
                    Label = worldName,
                    Platform = new PlatformRef(world, cell),
                    InfoPath = [kindLabel, worldName],
                });
            }
            if (kindNode.Children.Count > 0) parent.Children.Add(kindNode);
        }
    }

    private const byte PlatformCategory = 0x04;

    /// <summary>Appends drain bonus entrance subtrees for each drain state.</summary>
    private static void AddDrainNodes(TreeNode parent, ResourceLibrary library, GameData data)
    {
        if (!library.Contains(DrainCompositor.Sheet)) return;

        string drainLabel = EntityCategories.LabelOf(0x5B, data.EntityNames);
        var drainNode = new TreeNode { Label = drainLabel };
        foreach (var (state, label) in ((DrainState, string)[])
                 [(DrainState.Open, "Open"), (DrainState.Sealed, "Sealed")])
        {
            drainNode.Children.Add(new TreeNode
            {
                Label = label,
                Drain = state,
                InfoPath = [drainLabel, label],
            });
        }

        parent.Children.Add(drainNode);
    }

    /// <summary>Appends dig spot marker leaves for each resting sheet cell.</summary>
    private static void AddDigSpotNodes(TreeNode parent, ResourceLibrary library)
    {
        if (!library.Contains(DigSpotCompositor.Sheet)) return;

        var digSpotNode = new TreeNode { Label = "Dig Spot" };
        foreach (var (state, label) in ((DigSpotState, string)[])
                 [(DigSpotState.Exit, "Exit"), (DigSpotState.ExitUnderworld, "Exit (Underworld)"),
                  (DigSpotState.Bonus, "Bonus"), (DigSpotState.BonusSealed, "Bonus (Sealed)")])
        {
            digSpotNode.Children.Add(new TreeNode
            {
                Label = label,
                DigSpot = new DigSpotRef(state),
                InfoPath = ["Dig Spot", label],
            });
        }

        if (digSpotNode.Children.Count > 0) parent.Children.Add(digSpotNode);
    }

    /// <summary>Appends the water world exit sign leaf node.</summary>
    private static void AddExitSignNode(TreeNode parent, ResourceLibrary library)
    {
        if (!library.Contains(ExitSignCompositor.Sheet)) return;

        parent.Children.Add(new TreeNode
        {
            Label = "Exit Sign",
            ExitSign = true,
            InfoPath = ["Exit Sign"],
        });
    }

    /// <summary>Appends world-map signpost stills grouped by world, one leaf per signpost type.</summary>
    private static void AddWorldMapSignNodes(TreeNode parent, ResourceLibrary library, GameData data)
    {
        World[] worlds = [World.Caves, World.Water, World.Snow, World.Underworld];
        SignType[] types = [SignType.Level, SignType.Checkpoint, SignType.Trace, SignType.Gate, SignType.Draggo];

        var root = new TreeNode { Label = "World Map Signs" };
        foreach (var world in worlds)
        {
            if (!library.Contains(WorldMapSignCompositor.Sheet(world))) continue;

            string worldName = data.Nodes.WorldName(world);
            var worldNode = new TreeNode { Label = worldName };
            foreach (var type in types)
            {
                string label = SignTypeNames.Label(type);
                worldNode.Children.Add(new TreeNode
                {
                    Label = label,
                    WorldMapSign = new WorldMapSignRef(world, type),
                    InfoPath = ["World Map Signs", worldName, label],
                });
            }
            root.Children.Add(worldNode);
        }

        if (root.Children.Count > 0) parent.Children.Add(root);
    }

    /// <summary>Builds the interface and heads-up display animation branch.</summary>
    public static TreeNode? UiAndHud(SkinCatalog skins)
    {
        var root = new TreeNode { Label = "UI & HUD", IsExpanded = true };
        CharacterNodes.Add(root, skins,
            [AnimationTables.HudGeneral, AnimationTables.PowerUpBanners, AnimationTables.StatusIcons],
            collapseCharacter: false);
        return root.Children.Count > 0 ? root : null;
    }
}
