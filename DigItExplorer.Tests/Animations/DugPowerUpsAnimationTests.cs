using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Super Dug, Jetpack, and Dug Dirt animation tables and frame hashes against reference files.</summary>
public class DugPowerUpsAnimationTests
{
    [Fact]
    public void DugSuper_table_matches_transcription()
    {
        var set = AnimationTables.DugSuper;
        Assert.Equal("DugSuper", set.Name);
        Assert.Null(set.Category);
        Assert.Equal("DUG1A", set.FixedSheet);
        Assert.Equal("LVL000.PAL", set.FixedPalette);
        Assert.Single(set.Anims);
        Assert.Equal("super_dug_fly", set.Anims[0].Name);
        Assert.Equal(AnimMode.Loop, set.Anims[0].Mode);
        Assert.Equal([200, 201, 202, 203, 204, 205, 206, 207], set.Anims[0].Frames);
    }

    [Fact]
    public void DugJetpack_table_matches_transcription()
    {
        var set = AnimationTables.DugJetpack;
        Assert.Equal("DugJetpack", set.Name);
        Assert.Null(set.Category);
        Assert.Equal("DUG2A", set.FixedSheet);
        Assert.Equal("LVL000.PAL", set.FixedPalette);
        Assert.Equal(2, set.Anims.Count);
        Assert.Equal("jetpack_hover", set.Anims[0].Name);
        Assert.Equal([211, 210], set.Anims[0].Frames);
        Assert.Equal(AnimMode.Loop, set.Anims[0].Mode);
        Assert.Equal("jetpack_tilt", set.Anims[1].Name);
        Assert.Equal([210, 211, 212, 213, 214, 215, 216], set.Anims[1].Frames);
        Assert.Equal(AnimMode.Once, set.Anims[1].Mode);
    }

    [Fact]
    public void DugDirt_table_matches_transcription()
    {
        var set = AnimationTables.DugDirt;
        Assert.Equal("DugDirt", set.Name);
        Assert.Null(set.Category);
        Assert.Equal("DUGDIRT", set.FixedSheet);
        Assert.Equal("LVL000.PAL", set.FixedPalette);
        // Classified under Effects with Dug as its location.
        Assert.Equal("Dug Dirt", set.DisplayName);
        Assert.Equal("Dug (player)", set.FixedLocation);
        Assert.Single(set.Anims);
        Assert.Equal("dig_hole", set.Anims[0].Name);
        Assert.Equal(AnimMode.Once, set.Anims[0].Mode);
        Assert.Equal(Enumerable.Range(0, 27).ToArray(), set.Anims[0].Frames);
    }

    private static readonly (CharacterAnimSet Set, string Sheet, string Pal, string[] Shas)[] Reference =
    [
        (AnimationTables.DugSuper, "DUG1A.SPF", "LVL000.PAL", ["1026C86D76BABE701B3580F2075966906EBDDF58667177E021E591BC38E34EED"]),
        (AnimationTables.DugJetpack, "DUG2A.SPF", "LVL000.PAL",
        [
            "58744DDF124FB8F8E70171806EFC4F06C1C471931850D25EE9E334F053C6D5DC",
            "EEB9996F66E6800E45BC9A45E5480154EF5293EDD1B99CB81339EC58088F4DF8",
        ]),
        (AnimationTables.DugDirt, "DUGDIRT.SPF", "LVL000.PAL", ["F3ED41134DAFD2C6C5A2203EA49594355BF37A8B28C2A161136B22565B263394"]),
    ];

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void PowerUp_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        foreach (var (set, sheetFile, palName, shas) in Reference)
        {
            var pages = SheetImage.Read(library.Read(sheetFile)).Frames;
            var palette = VgaPalette.From6Bit(library.Read(palName).AsSpan(0, 768));

            AnimationGuard.AssertRectHashes(set, pages, palette, shas);
        }
    }
}
