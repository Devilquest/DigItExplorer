using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Dug crouch and crawl animation tables and rendered frame hashes against reference files.</summary>
public class DugCrouchAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("crouch_down", [50, 51, 52, 53, 54], AnimMode.Once),
        ("crawl", [54, 55, 56, 57, 58, 59, 60, 61, 62, 63], AnimMode.Loop),
        ("crawl_blocked", [53, 54], AnimMode.Loop),
        ("crouch_up", [54, 53, 52, 51, 50], AnimMode.Once),
    ];

    private static readonly (char Code, string Sheet, string Pal, string[] Shas)[] Reference =
    [
        ('0', "DUG0B.SPF", "LVL000.PAL",
        [
            "E47BFE1DC79EE87F4981734ED5571B6329E57CD8F5B92D608A68F708AFB93881",
            "5555B538E240214975236E24B6E3AF50D27C458ABE96AD039AB4211C7EE7DFD9",
            "D73EF9E6CE3455C0C232E5047A424826886D0F68B57EB3D37DCC9661BF6B270A",
            "37514977C7E3A6CD9BD3152E216445689BFC866EBFBEDAC71F713C8EF078C1B3",
        ]),
        ('6', "DUG6B.SPF", "LVL400.PAL",
        [
            "B4C60CCCDC16D07B931DD0DFDAA6ABC0577FDA50BB74B187781B397F925E3974",
            "0C81ECB8F8C8193F0DCB423827F92236E1A282FCDB03CB0EB23AA593CFD887DE",
            "3827C8FEC6D1F9DB13A0A3756E5CFA1CB96744720C49F689F8B6228B590D4E2B",
            "A4DF27DE6D8B48135E24488CEDB65CF75F8C42621AA525B5CFB6BB156B9257E9",
        ]),
        ('3', "DUG3B.SPF", "LVL600.PAL",
        [
            "34A72518B1722CF032A48E363D4244BFBD02D75E53476FE01C8F9D5477564DBC",
            "DD68EA589534A66DCD7D5068F6C0485E5D739DB8D11A549405126993BADE295E",
            "F4B98C9E2BC8B6C459172B804C2827690217E6E826D586114772254A8CDC83C5",
            "3224DC7816A8DDE506811AAD47A91E044A1A9393542D8C6884F06E8B1D0402A9",
        ]),
        ('5', "DUG5B.SPF", "LVL000.PAL",
        [
            "0FB551BEEBFAC22A729BA1216E0624B031BF70D671185518EC9BD3A4AAC66F90",
            "E4F8F32745B7244D3427ED24BC1D4AD8EDD6280B19C57E451BEB24434D527442",
            "E75568CC53F66C9657BAB4D4098D4CA80C2EC9F01D32AB9F1EFBB3E2B60AD4C1",
            "7F5B6783C631BE8C5C141BD6AC68A0AE02306D05606C72F6A1B190F2CB26F2E2",
        ]),
    ];

    [Fact]
    public void DugCrouch_table_matches_transcription()
    {
        var set = AnimationTables.DugCrouch;
        Assert.Equal("DugCrouch", set.Name);
        Assert.Null(set.Category);
        Assert.Equal('B', set.SheetLetter);
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void DugCrouch_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var set = AnimationTables.DugCrouch;
        foreach (var (code, sheetFile, palName, shas) in Reference)
        {
            var pages = SheetImage.Read(library.Read(sheetFile)).Frames;
            var palette = VgaPalette.From6Bit(library.Read(palName).AsSpan(0, 768));

            AnimationGuard.AssertRectHashes(set, pages, palette, shas);
        }
    }
}
