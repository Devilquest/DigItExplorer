using DigItExplorer.App.Models;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.App.Services.ResourceTree;

/// <summary>Constructs resource tree branches for player, enemy, item, and effect animations.</summary>
internal static class CharacterBranches
{
    // FireballUnused borrows Pyrosaur's DLF category to resolve its sheet file.
    public static readonly HashSet<string> EffectsWithBorrowedCategory = [AnimationTables.FireballUnused.Name];

    /// <summary>Builds the player character branch structured by costume and action categories.</summary>
    public static TreeNode? Player(SkinCatalog skins, GameData data)
    {
        var root = new TreeNode { Label = "Dug (player)", IsExpanded = true };

        var skinCache = new Dictionary<string, IReadOnlyList<CharacterSkin>>();
        foreach (var set in (CharacterAnimSet[])
            [AnimationTables.Dug, AnimationTables.DugCrouch, AnimationTables.DugWait,
             AnimationTables.DugDig, AnimationTables.DugSwim,
             AnimationTables.DugSuper, AnimationTables.DugJetpack, AnimationTables.DugMap])
            skinCache[set.Name] = skins.SkinsOf(set);

        CharacterSkin? SkinFor(CharacterAnimSet s, string suffix)
            => skinCache.TryGetValue(s.Name, out var list)
               ? list.FirstOrDefault(sk => sk.Suffix == suffix) : null;

        // context is what keeps one costume's "Walk" apart from another's, so the category nodes stay out
        // of it: they group animations that are already distinct, and a costume is the same one drawn again.
        void AddLeaves(TreeNode parent, string suffix, string[] context,
            (CharacterAnimSet Set, int Index)[] slots)
        {
            foreach (var (set, idx) in slots)
            {
                string sfx = set.FixedSheet is not null ? "" : suffix;
                var skin = SkinFor(set, sfx);
                if (skin is null || !skin.Present[idx]) continue;
                var animLabel = skins.AnimLabel(set, set.Anims[idx]);
                parent.Children.Add(new TreeNode
                {
                    Label = animLabel,
                    Animation = new AnimationRef(set.Name, sfx, idx),
                    InfoPath = [.. context, animLabel],
                });
            }
        }

        static (CharacterAnimSet, int)[] AllOf(CharacterAnimSet s)
        {
            var a = new (CharacterAnimSet, int)[s.Anims.Count];
            for (int i = 0; i < a.Length; i++) a[i] = (s, i);
            return a;
        }

        // Same wording as SkinCatalog's own costume-5 label, so the tree node and the info bar's world
        // field say the same thing.
        string CostumeLabel(PlayerCostume c) => c.World is World w
            ? data.Nodes.WorldName(w)
            : "Unused costume";

        (string Label, (CharacterAnimSet Set, int Index)[] Slots)[] categories =
        [
            ("Basic Movement", [
                (AnimationTables.Dug, 0),
                (AnimationTables.Dug, 1),
                (AnimationTables.Dug, 2),
                (AnimationTables.Dug, 8),
                (AnimationTables.Dug, 9),
                (AnimationTables.Dug, 10),
                (AnimationTables.DugWait, 0),
                (AnimationTables.DugWait, 1),
            ]),
            ("Jump & Acrobatics", [
                (AnimationTables.Dug, 3),
                (AnimationTables.Dug, 4),
                (AnimationTables.Dug, 5),
                (AnimationTables.Dug, 6),
                (AnimationTables.Dug, 7),
                (AnimationTables.Dug, 11),
            ]),
            ("Crouch & Crawl", [
                (AnimationTables.DugCrouch, 0),
                (AnimationTables.DugCrouch, 1),
                (AnimationTables.DugCrouch, 2),
                (AnimationTables.DugCrouch, 3),
            ]),
            ("Dig", [
                (AnimationTables.DugDig, 0),
                (AnimationTables.DugDig, 1),
                (AnimationTables.DugDig, 2),
            ]),
        ];

        for (int ci = 0; ci < PlayerCostumes.All.Count; ci++)
        {
            if (ci == 1)
            {
                var gwNode = new TreeNode { Label = "Great Waters" };
                AddLeaves(gwNode, "", ["Dug", "Great Waters"], [
                    (AnimationTables.DugSwim, 0),
                    (AnimationTables.DugSwim, 2),
                ]);
                if (gwNode.Children.Count > 0) root.Children.Add(gwNode);
            }

            var costume = PlayerCostumes.All[ci];
            string suffix = costume.Code.ToString();
            var costumeNode = new TreeNode { Label = CostumeLabel(costume) };

            foreach (var (catLabel, slots) in categories)
            {
                var catNode = new TreeNode { Label = catLabel };
                AddLeaves(catNode, suffix, ["Dug", CostumeLabel(costume)], slots);
                if (catNode.Children.Count > 0) costumeNode.Children.Add(catNode);
            }
            if (costumeNode.Children.Count > 0) root.Children.Add(costumeNode);
        }

        {
            var saNode = new TreeNode { Label = "Special Abilities" };
            AddLeaves(saNode, "", ["Dug", "Special Abilities"], [
                (AnimationTables.DugSuper, 0),
                (AnimationTables.DugJetpack, 0),
                (AnimationTables.DugJetpack, 1),
            ]);
            if (saNode.Children.Count > 0) root.Children.Add(saNode);
        }

        {
            var wmNode = new TreeNode { Label = "World Map" };
            var mapSlots = AllOf(AnimationTables.DugMap);

            for (int ci = 0; ci < PlayerCostumes.All.Count; ci++)
            {
                if (ci == 1)
                {
                    var gwMapNode = new TreeNode { Label = "Great Waters" };
                    AddLeaves(gwMapNode, "", ["Dug", "World Map", "Great Waters"], [(AnimationTables.DugSwim, 1)]);
                    if (gwMapNode.Children.Count > 0) wmNode.Children.Add(gwMapNode);
                }

                var costume = PlayerCostumes.All[ci];
                var mapCostumeNode = new TreeNode { Label = CostumeLabel(costume) };
                AddLeaves(mapCostumeNode, costume.Code.ToString(),
                    ["Dug", "World Map", CostumeLabel(costume)], mapSlots);
                if (mapCostumeNode.Children.Count > 0) wmNode.Children.Add(mapCostumeNode);
            }
            if (wmNode.Children.Count > 0) root.Children.Add(wmNode);
        }

        return root.Children.Count > 0 ? root : null;
    }

    /// <summary>Builds a character branch for animated entity sets filtered by predicate.</summary>
    public static TreeNode? Characters(SkinCatalog skins, string rootLabel, Func<CharacterAnimSet, bool> setFilter)
    {
        var root = new TreeNode { Label = rootLabel, IsExpanded = true };
        var matchedSets = AnimationTables.All.Values.Where(setFilter).ToList();
        CharacterNodes.Add(root, skins, matchedSets, collapseCharacter: matchedSets.Count == 1);
        return root.Children.Count > 0 ? root : null;
    }

    /// <summary>Builds the goodies branch: the gold, silver, and gem pickups.</summary>
    public static TreeNode? Goodies(SkinCatalog skins)
    {
        CharacterAnimSet[] sets = [AnimationTables.GoldItems, AnimationTables.SilverItems, AnimationTables.Gems];
        var root = new TreeNode { Label = "Goodies", IsExpanded = true };
        CharacterNodes.Add(root, skins, sets, collapseCharacter: sets.Length == 1);
        return root.Children.Count > 0 ? root : null;
    }

    /// <summary>Builds the visual effects, projectiles, and particle animation branch.</summary>
    public static TreeNode? Effects(SkinCatalog skins)
    {
        var root = new TreeNode { Label = "Effects & VFX", IsExpanded = true };

        var projectiles = new TreeNode { Label = "Projectiles" };
        CharacterNodes.Add(projectiles, skins,
            [AnimationTables.Fireball, AnimationTables.FireballUnused], collapseCharacter: false);
        if (projectiles.Children.Count > 0) root.Children.Add(projectiles);

        var impact = new TreeNode { Label = "Impact & Particles" };
        CharacterNodes.Add(impact, skins,
            [AnimationTables.GeneralEffects, AnimationTables.Hit, AnimationTables.Sparkles,
             AnimationTables.DugDirt], collapseCharacter: false);
        if (impact.Children.Count > 0) root.Children.Add(impact);

        return root.Children.Count > 0 ? root : null;
    }
}
