using System.IO;
using System.Text;
using DigItExplorer.App.Models;
using DigItExplorer.Core.Catalog;
using DigItExplorer.App.Services.ResourceTree;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;

namespace DigItExplorer.App.Services;

/// <summary>Constructs hierarchical resource navigation trees for raw files and semantic game categories.</summary>
internal static class ResourceTreeBuilder
{
    /// <summary>Builds a flat tree grouping resources by file extension for the Raw tab.</summary>
    public static List<TreeNode> BuildFamilyTree(IEnumerable<string> names)
    {
        var roots = new List<TreeNode>();
        foreach (var family in names.GroupBy(FamilyOf).OrderBy(g => g.Key, NaturalStringComparer.Instance))
        {
            var files = family.OrderBy(n => n, NaturalStringComparer.Instance).ToList();
            // The count is carried rather than written into the label, so a search can report how much of the
            // family it is showing without the label having to be rebuilt to say it.
            var node = new TreeNode { Label = family.Key, TotalCount = files.Count };
            foreach (var name in files)
                node.Children.Add(new TreeNode { Label = name, Resource = name, IsRawFile = true, InfoPath = [family.Key] });
            roots.Add(node);
        }
        return roots;
    }

    /// <summary>Builds the semantic resource tree encompassing worlds, screens, characters, goodies, and audio.</summary>
    public static List<TreeNode> BuildResourcesTree(ResourceLibrary library, SkinCatalog skins, GameData data)
    {
        var names = library.Names;
        var present = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

        var music = names.Where(n => HasExt(n, ".DAT") && !n.Equals("GPALFIX.DAT", StringComparison.OrdinalIgnoreCase));
        var sfx = names.Where(n => HasExt(n, ".SMP"));

        var audio = new TreeNode { Label = "Audio", IsExpanded = true };
        AddIfAny(audio, Group("Music", music, AudioLabel,
            sinkToBottom: n => KnownResources.Unused.Contains(n, StringComparer.OrdinalIgnoreCase)));
        AddIfAny(audio, Group("Sound Effects", sfx, Path.GetFileNameWithoutExtension));

        var worlds = WorldsBranch.Build(names, library, data);
        var screens = ScreensBranch.Build(present, data, skins);
        var player = CharacterBranches.Player(skins, data);
        var enemies = CharacterBranches.Characters(skins, "Enemies",
            set => set.Category is not null && !CharacterBranches.EffectsWithBorrowedCategory.Contains(set.Name));
        var objectsAndHazards = ObjectAndUiBranches.ObjectsAndHazards(skins, library, data);
        var goodies = CharacterBranches.Goodies(skins);
        var effects = CharacterBranches.Effects(skins);
        var uiAndHud = ObjectAndUiBranches.UiAndHud(skins);

        var branches = new List<TreeNode>();
        if (worlds is not null) branches.Add(worlds);
        if (screens is not null) branches.Add(screens);
        if (player is not null) branches.Add(player);
        if (enemies is not null) branches.Add(enemies);
        if (objectsAndHazards is not null) branches.Add(objectsAndHazards);
        if (goodies is not null) branches.Add(goodies);
        if (effects is not null) branches.Add(effects);
        if (uiAndHud is not null) branches.Add(uiAndHud);
        if (audio.Children.Count > 0) branches.Add(audio);
        return branches;
    }

    private static void AddIfAny(TreeNode parent, TreeNode group)
    {
        if (group.Children.Count > 0) parent.Children.Add(group);
    }

    /// <summary>Builds a flat leaf node group with optional sorting and label decoration.</summary>
    private static TreeNode Group(string label, IEnumerable<string> names,
                                  Func<string, string>? decorate = null, Func<string, bool>? sinkToBottom = null)
    {
        var files = sinkToBottom is null
            ? names.OrderBy(n => n, NaturalStringComparer.Instance)
            : names.OrderBy(n => sinkToBottom(n)).ThenBy(n => n, NaturalStringComparer.Instance);
        var node = new TreeNode { Label = label };
        foreach (var name in files)
            node.Children.Add(new TreeNode { Label = decorate?.Invoke(name) ?? name, Resource = name, InfoPath = [label] });
        return node;
    }

    /// <summary>Formats an audio file label with unused track annotations.</summary>
    private static string AudioLabel(string name)
    {
        var cleanName = Path.GetFileNameWithoutExtension(name);
        if (KnownResources.AudioDuplicateOf.TryGetValue(name, out var twin))
        {
            var cleanTwin = Path.GetFileNameWithoutExtension(twin);
            return $"{cleanName}  (unused) (={cleanTwin})";
        }
        return KnownResources.Unused.Contains(name, StringComparer.OrdinalIgnoreCase)
            ? $"{cleanName}  (unused)"
            : cleanName;
    }

    private static string FamilyOf(string name)
    {
        var ext = Path.GetExtension(name);
        return ext.Length > 0 ? ext.TrimStart('.').ToUpperInvariant() : "(none)";
    }

    private static bool HasExt(string name, string ext)
        => Path.GetExtension(name).Equals(ext, StringComparison.OrdinalIgnoreCase);
}
