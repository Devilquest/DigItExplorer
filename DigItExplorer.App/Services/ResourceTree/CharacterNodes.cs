using DigItExplorer.App.Models;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.App.Services.ResourceTree;

/// <summary>Builds hierarchical character, skin, and animation leaf tree nodes.</summary>
internal static class CharacterNodes
{
    /// <summary>Appends character and skin animation leaf nodes under the specified parent node.</summary>
    public static void Add(TreeNode parent, SkinCatalog skins,
        IReadOnlyList<CharacterAnimSet> sets, bool collapseCharacter)
    {
        foreach (var set in sets)
        {
            var skinList = skins.SkinsOf(set);
            if (skinList.Count == 0) continue;

            var charNode = collapseCharacter ? parent : new TreeNode { Label = skins.CharacterLabel(set) };

            bool collapseSkin = skinList.Count == 1;
            var characterLabel = skins.CharacterLabel(set);
            foreach (var (suffix, label, present) in skinList)
            {
                var skinNode = collapseSkin ? charNode : new TreeNode { Label = label };
                for (int i = 0; i < set.Anims.Count; i++)
                {
                    if (!present[i]) continue; // absent in this skin's sheet: don't list it
                    var animLabel = skins.AnimLabel(set, set.Anims[i]);

                    skinNode.Children.Add(new TreeNode
                    {
                        Label = animLabel,
                        Animation = new AnimationRef(set.Name, suffix, i),
                        InfoPath = [characterLabel, label, animLabel],
                    });
                }
                if (!collapseSkin && skinNode.Children.Count > 0) charNode.Children.Add(skinNode);
            }
            if (!collapseCharacter && charNode.Children.Count > 0) parent.Children.Add(charNode);
        }
    }
}
