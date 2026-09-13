using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins animation tables and rendered frame hashes for all six Aquatics species.</summary>
public class AquaticsAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("swim", [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], AnimMode.Loop),
        ("turn", [10, 11, 12, 13, 14], AnimMode.Once),
    ];

    // Per species, one SHA-256 per animation over its concatenated RGBA frames, in table order.
    private static readonly (string Character, string[] Shas)[] Reference =
    [
        ("Aqua Slugger",
        [
            "2F149A1AF076F76C2B10B53C66413D63B311D0B0581D02EE75A8065E1B0B0E9A",
            "49C09239F9DE24EF4BCDFCB895E7A7945D56A41BB5FFC0E3355270CE50B7FC52",
        ]),
        ("Sea Draggo",
        [
            "98B7F9057658BEF9574DCF61DBDA176EA65ED33BF22DD92EAECAD08940DC6E35",
            "52A04EB3E3DE2EFF7BD52B1114B024014EAD2BD61B3C2B93BFF1BB7A3AE97053",
        ]),
        ("Rockerfish",
        [
            "3C27597B6730B016B684EAEF6EFC635ADBB9197DC7E02443CC4B0EB54275FA4F",
            "E81447EF3F36128D749077AF6AC558E1D3E0D3624D96543B5745847F8565EB56",
        ]),
        ("Sea Spurk",
        [
            "D0AA6553BC41C31D08E247E9CBE8C88D5558B62BAC533EAF6AD9BE8850F149A0",
            "17FC222C0B69DAB55D0E7431ADA7CE7B4E4A677534125521D1C43083CA2C35FB",
        ]),
        ("Hopperfish",
        [
            "AAF9E3D007B9D14D5EBAA1C547DF8008F1A0957E9E7F240D91C9088D497D6220",
            "5A753DD3780D6911489BDA3711B22FA1CCE902F038FC5AF4B60FEBF8FDB65BC0",
        ]),
        ("Nirpies",
        [
            "590F87687EB28D1D1CEC0051DFF25560F98423D02051A6C4ABD5E339ACC1BF29",
            "083E440C15880049843668CA34A6EBCFB853F225D2ED4A3A57A97B19D5F7500B",
        ]),
    ];

    private static readonly (string Name, CharacterAnimSet Set)[] Sets =
    [
        ("Aqua Slugger", AnimationTables.AquaSlugger),
        ("Sea Draggo", AnimationTables.SeaDraggo),
        ("Rockerfish", AnimationTables.Rockerfish),
        ("Sea Spurk", AnimationTables.SeaSpurk),
        ("Hopperfish", AnimationTables.Hopperfish),
        ("Nirpies", AnimationTables.Nirpies),
    ];

    [Theory]
    [MemberData(nameof(SetNames))]
    public void Fish_table_matches_reference_transcription(string name)
    {
        var set = Sets.First(s => s.Name == name).Set;
        Assert.Equal((byte)0x12, set.Category);
        Assert.Equal("WO_FISH", set.FixedSheet);
        Assert.Equal("LVL200.PAL", set.FixedPalette);
        Assert.Equal(World.Water, set.FixedLocationWorld); // the only world Aquatics appear in
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Fact]
    public void All_six_species_share_the_same_animation_table_instance()
    {
        foreach (var (_, set) in Sets.Skip(1))
            Assert.Same(AnimationTables.AquaSlugger.Anims, set.Anims);
    }

    [Fact]
    public void Rects_are_the_literal_per_species_grid_formulas()
    {
        // First and last cell of each block, so a wrong stride or column count fails here rather than
        // passing a count check with the cells in the wrong places.
        Assert.Equal(new FrameRect(0, 0, 142, 32, 18), AnimationTables.AquaSlugger.Rects![0]);
        Assert.Equal(new FrameRect(0, 165, 161, 32, 18), AnimationTables.AquaSlugger.Rects[14]); // i=14 -> col 5, row 1 (cols=9)

        Assert.Equal(new FrameRect(0, 0, 42, 39, 34), AnimationTables.SeaDraggo.Rects![0]);
        Assert.Equal(new FrameRect(0, 240, 77, 39, 34), AnimationTables.SeaDraggo.Rects[14]); // i=14 -> col 6, row 1 (cols=8)

        Assert.Equal(new FrameRect(0, 0, 0, 34, 20), AnimationTables.Rockerfish.Rects![0]);
        Assert.Equal(new FrameRect(0, 175, 21, 34, 20), AnimationTables.Rockerfish.Rects[14]); // i=14 -> col 5, row 1 (cols=9)

        Assert.Equal(new FrameRect(1, 0, 24, 34, 20), AnimationTables.SeaSpurk.Rects![0]);
        Assert.Equal(new FrameRect(1, 175, 45, 34, 20), AnimationTables.SeaSpurk.Rects[14]); // i=14 -> col 5, row 1 (cols=9)

        Assert.Equal(new FrameRect(0, 0, 112, 31, 14), AnimationTables.Hopperfish.Rects![0]);
        Assert.Equal(new FrameRect(0, 128, 127, 31, 14), AnimationTables.Hopperfish.Rects[14]); // i=14 -> col 4, row 1 (cols=10)

        Assert.Equal(new FrameRect(1, 0, 0, 23, 11), AnimationTables.Nirpies.Rects![0]);
        Assert.Equal(new FrameRect(1, 24, 12, 23, 11), AnimationTables.Nirpies.Rects[14]); // i=14 -> col 1, row 1 (cols=13)

        foreach (var (_, set) in Sets)
            for (int i = 0; i < 15; i++)
                Assert.True(set.Rects!.ContainsKey(i));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory(GameNeeds.FullResourceSet)]
    [MemberData(nameof(SetNames))]
    public void Fish_frame_pixels_match_reference_pixels(string name)
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var set = Sets.First(s => s.Name == name).Set;
        var shas = Reference.First(r => r.Character == name).Shas;

        var pages = SheetImage.Read(library.Read("WO_FISH.MPF")).Frames;
        var palette = VgaPalette.From6Bit(library.Read("LVL200.PAL").AsSpan(0, 768));

        AnimationGuard.AssertRectHashes(set, pages, palette, shas);
    }

    /// <summary>Species names, for xUnit's per-case <c>[Theory]</c> display.</summary>
    public static IEnumerable<object[]> SetNames() => Sets.Select(s => new object[] { s.Name });
}
