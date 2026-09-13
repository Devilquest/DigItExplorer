using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Knowledge.Info;

/// <summary>Computes per-category entity counts for level and menu documents.</summary>
internal static class EntityCensus
{
    /// <summary>Generates info rows counting entity instances per category.</summary>
    public static IEnumerable<InfoRow> Rows(IReadOnlyList<DlfRecord> entities, EntityNames? entityNames)
        => entities
            .GroupBy(e => e.Category)
            .OrderBy(g => g.Key)
            .Select(g => new InfoRow(EntityCategories.LabelOf(g.Key, entityNames), g.Count().ToString()));
}
