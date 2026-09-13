using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies category classification and subtype naming in <see cref="EntityCategories"/> against shipped levels.</summary>
public class EntityCategoryTests
{
    /// <summary>Verifies that all entity categories present in shipped DLF level files map to known buckets.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void No_category_a_shipped_level_carries_falls_through_to_Misc()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var dlfs = library.Names.Where(n =>
            n.StartsWith("LVL", StringComparison.OrdinalIgnoreCase) &&
            n.EndsWith(".DLF", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.NotEmpty(dlfs);

        var categories = dlfs
            .SelectMany(name => DlfRecord.ReadAll(library.Read(name)))
            .Select(r => r.Category)
            .ToHashSet();

        var unbucketed = categories.Where(c => EntityCategories.BucketOf(c) == EntityBucket.Misc)
            .Select(c => $"0x{c:X2}")
            .Order()
            .ToList();

        Assert.True(unbucketed.Count == 0, $"categories with no bucket: {string.Join(", ", unbucketed)}");
    }

    /// <summary>Verifies that every entity bucket is populated by categories from shipped level files.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Every_bucket_is_reachable_from_a_shipped_level()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var buckets = library.Names.Where(n =>
                n.StartsWith("LVL", StringComparison.OrdinalIgnoreCase) &&
                n.EndsWith(".DLF", StringComparison.OrdinalIgnoreCase))
            .SelectMany(name => DlfRecord.ReadAll(library.Read(name)))
            .Select(r => EntityCategories.BucketOf(r.Category))
            .ToHashSet();

        foreach (var bucket in (EntityBucket[])[EntityBucket.Mechanisms, EntityBucket.Enemies,
                     EntityBucket.Goodies, EntityBucket.Decor, EntityBucket.Markers])
        {
            Assert.Contains(bucket, buckets);
        }
    }

    /// <summary>Verifies category bucket assignment for mechanisms (Drain 0x5B, Platforms 0x04) and markers (Exit 0x5A).</summary>
    [Fact]
    public void The_drain_is_a_mechanism()
    {
        Assert.Equal(EntityBucket.Mechanisms, EntityCategories.BucketOf(0x5B));
        Assert.Equal(EntityBucket.Mechanisms, EntityCategories.BucketOf(0x04));
        Assert.Equal(EntityBucket.Markers, EntityCategories.BucketOf(0x5A)); // the exit sign stays one
    }

    /// <summary>Verifies subtype naming for category 0x04 (Moving Platform type 0, Falling Platform type 1).</summary>
    [Fact]
    public void A_platform_is_named_by_its_kind_and_its_category_has_no_name_of_its_own()
    {
        Assert.Equal("Moving Platform", EntityCategories.SubtypeNameOf(0x04, 0));
        Assert.Equal("Falling Platform", EntityCategories.SubtypeNameOf(0x04, 1));
        Assert.Equal("0x04", EntityCategories.LabelOf(0x04));
    }
}
