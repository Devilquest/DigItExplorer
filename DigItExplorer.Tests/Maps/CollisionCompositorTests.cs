using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Maps;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Guards <see cref="CollisionCompositor"/> and <see cref="ColliderStamper"/> against frozen
/// per-level pixels.</summary>
public class CollisionCompositorTests
{
    /// <summary>Per-level reference SHA-256 hashes of unstamped collision layers.</summary>
    private static readonly (string Level, string Sha)[] Reference =
    [
        ("LVL000", "F00F14FC3B51D02379E19847516881EAC41197C1D0CCA1C6A0A37A15D81B8915"), // caves  4x3
        ("LVL010", "71737AEFD9634D2287A06925227949A698C16C6841000B5284131CFCD827CC47"), // caves  4x3
        ("LVL090", "BBA0656FC9FF9AF83CF7B649F56A665F7B747411A47B27F0D276F65FD7CADCEB"), // caves  9x2, wide
        ("LVL400", "91502D1E1DE7C28711074CB9A3655E0D2F9E62201C90ED5A0764174984D1D34C"), // snow   8x2
        ("LVL630", "33B167AC428A4D126FC278CE912FD423E34BDE16DE14E99D2E473775C8BCAB34"), // under  2x4, tall
        ("LVL750", "2C59CDD6AD1C52C4EE91D529DF5480EA4C885C01F89E7DD156CEEA140FCFBA05"), // boss   3x2
        ("LVL900", "BAB8F54F884557A225551ECCFCAB010A789ACCA3C321200EBB7544A0953120E8"), // menu   2x1, no crop
    ];

    /// <summary>Per-level reference SHA-256 hashes of collision layers after collider footprints are applied.</summary>
    private static readonly (string Level, string Sha)[] StampedReference =
    [
        ("LVL200", "C5C4D8B260391DA4263131ABD106306528E35A50CC7F3BDEFAC1518BACF558BE"), // water, drain
        ("LVL020", "AF69DB2AF206F1F9AED3DE4CBAC3E0CF71DFEE4DB8FC6F8E97DB22AB958CC2FE"), // caves, moving platform
        ("LVL209", "23F40A6101C9C9166EA04700AAD071F225D67551A91E36147E401AC9479F6685"), // water, negative-X drain
    ];

    /// <summary>Guards raw collision plane composition against reference pixel hashes across sample levels.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Collision_reconstruction_matches_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        foreach (var (level, expected) in Reference)
        {
            Assert.True(library.Contains($"{level}M.MPF"), $"{level}M.MPF missing in {gameDir}");

            var dlf = library.TryRead($"{level}.DLF");

            var collision = CollisionCompositor.Compose(library.Read($"{level}M.MPF"), dlf);
            var actual = Convert.ToHexString(SHA256.HashData(collision.ToRgba32()));

            Assert.Equal(expected, actual, ignoreCase: true);
        }
    }

    /// <summary>Guards collision layer composition with stamped platform and drain colliders against reference hashes.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet, Intact = ["LVL020M.MPF"])]
    public void Collider_stamping_matches_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        foreach (var (level, expected) in StampedReference)
        {
            Assert.True(library.Contains($"{level}M.MPF"), $"{level}M.MPF missing in {gameDir}");

            var dlfBytes = library.Read($"{level}.DLF");
            var records = DlfRecord.ReadAll(dlfBytes);

            var collision = CollisionCompositor.Compose(library.Read($"{level}M.MPF"), dlfBytes);

            var woMvl = ResolveWoMvl(library.TryRead, level);
            if (woMvl is not null)
                ColliderStamper.StampMovingPlatforms(collision, records, woMvl);

            var woDrain = library.TryRead("WO_DRAIN.SPF");
            if (woDrain is not null)
                ColliderStamper.StampDrains(collision, records, woDrain);

            var actual = Convert.ToHexString(SHA256.HashData(collision.ToRgba32()));
            Assert.Equal(expected, actual, ignoreCase: true);
        }
    }

    /// <summary>Guards that loaded map documents keep base collision planes unstamped until rendered.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet, Intact = ["LVL020M.MPF"])]
    public void Loading_a_level_leaves_its_collision_plane_unstamped()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        foreach (var (level, stamped) in StampedReference)
        {
            var map = MapDocument.Load(level, loader);
            Assert.NotNull(map);
            Assert.NotNull(map!.Collision);
            Assert.NotEmpty(map.ColliderStamps);

            var bare = CollisionCompositor.Compose(loader($"{level}M.MPF")!, loader($"{level}.DLF"));
            var actual = Convert.ToHexString(SHA256.HashData(map.Collision!.ToRgba32()));

            Assert.Equal(Convert.ToHexString(SHA256.HashData(bare.ToRgba32())), actual, ignoreCase: true);
            Assert.NotEqual(stamped, actual, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>Guards that document collider stamps preserve underlying solid codes across transparent sprite borders.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet, Intact = ["LVL020M.MPF"])]
    public void A_documents_footprints_differ_from_the_reference_only_where_it_overwrote_the_level()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        foreach (var (level, _) in StampedReference)
        {
            var map = MapDocument.Load(level, loader);
            Assert.NotNull(map?.Collision);

            var bare = CollisionCompositor.Compose(loader($"{level}M.MPF")!, loader($"{level}.DLF")).MaterialCodes;
            var reference = ReferenceStamped(loader, level).MaterialCodes;

            var ours = (byte[])bare.Clone();
            ColliderStamper.Apply(ours, map!.Collision!.Width, map.Collision.Height, map.ColliderStamps);

            int passedOver = 0;
            for (int i = 0; i < ours.Length; i++)
            {
                if (ours[i] == reference[i]) continue;
                Assert.Equal((byte)15, reference[i]); // the reference only ever differs by having written open
                Assert.Equal(bare[i], ours[i]); // and where it did, the level's own code is what we keep
                passedOver++;
            }

            // Without this the assertions above are satisfied by a document whose footprints do nothing at
            // all. Tied to whether this level actually has something to pass over, which makes it the check
            // that the platform levels, where nothing is transparent, still match the reference exactly.
            bool hasTransparentBorder = map.ColliderStamps.Any(s => s.TransparentCode is not null);
            Assert.Equal(hasTransparentBorder, passedOver > 0);
        }
    }

    /// <summary>Guards that every level collision plane matches its terrain dimensions exactly.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Every_levels_collision_plane_is_the_same_size_as_its_terrain()
    {
        using var library = OpenLibrary();

        var levels = library.Names
            .Where(n => n.StartsWith("LVL", StringComparison.OrdinalIgnoreCase)
                     && n.EndsWith("M.MPF", StringComparison.OrdinalIgnoreCase))
            .Select(n => n[..^5])
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.NotEmpty(levels);

        foreach (var level in levels)
        {
            var dlf = library.TryRead($"{level}.DLF");
            var collision = CollisionCompositor.Compose(library.Read($"{level}M.MPF"), dlf);
            var terrain = TerrainCompositor.Compose(library.Read($"{level}F.MPF"), dlf);

            Assert.Equal((terrain.Width, terrain.Height), (collision.Width, collision.Height));
            Assert.Equal((terrain.Cols, terrain.Rows), (collision.Cols, collision.Rows));
        }
    }

    /// <summary>Guards that surplus collision blocks in LVL094M are ignored in favor of the declared header grid.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void LVL094s_surplus_collision_blocks_are_ignored()
    {
        using var library = OpenLibrary();

        var sheet = SheetImage.Read(library.Read("LVL094M.MPF"));
        Assert.Equal(18, sheet.Frames.Count); // the surplus this test is about

        var collision = CollisionCompositor.Compose(library.Read("LVL094M.MPF"), library.TryRead("LVL094.DLF"));

        Assert.Equal((3, 4, 12), (collision.Cols, collision.Rows, collision.BlockCount));
        Assert.Equal((800, 800), (collision.Width, collision.Height));

        // Composed from the right twelve, not merely the right count of them: the surplus blocks are not
        // collision data at all, so every code in the plane landing in the material set pins that down.
        var material = new HashSet<byte> { 0, 15, 16, 32, 48, 64, 80, 112 };
        Assert.All(collision.MaterialCodes, code => Assert.Contains(code, material));
    }

    /// <summary>The fully-stamped plane, which is the form <see cref="StampedReference"/>'s hashes were
    /// taken from and a document deliberately does not produce.</summary>
    private static CollisionImage ReferenceStamped(Func<string, byte[]?> loader, string level)
    {
        var dlfBytes = loader($"{level}.DLF")!;
        var records = DlfRecord.ReadAll(dlfBytes);
        var collision = CollisionCompositor.Compose(loader($"{level}M.MPF")!, dlfBytes);

        var woMvl = ResolveWoMvl(loader, level);
        if (woMvl is not null)
            ColliderStamper.StampMovingPlatforms(collision, records, woMvl);

        var woDrain = loader("WO_DRAIN.SPF");
        if (woDrain is not null)
            ColliderStamper.StampDrains(collision, records, woDrain);

        return collision;
    }

    private static ResourceLibrary OpenLibrary()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        return ResourceLibrary.Open(gameDir);
    }

    private static byte[]? ResolveWoMvl(Func<string, byte[]?> loader, string level)
    {
        int world = level.Length > 3 && char.IsDigit(level[3]) ? (level[3] - '0') / 2 : 0;
        foreach (var name in new[] { $"WO_MVL0{world}", "WO_MVL", "WO_MVL00" })
        foreach (var ext in new[] { ".SPF", ".MPF" })
        {
            if (loader(name + ext) is { } bytes) return bytes;
        }
        return null;
    }
}
