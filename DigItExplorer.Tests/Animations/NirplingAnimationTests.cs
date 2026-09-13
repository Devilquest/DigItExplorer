using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Nirpling animation tables and rendered frame hashes against reference files.</summary>
public class NirplingAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("run", [0, 1, 2, 3], AnimMode.Loop),
        ("hop_rise", [4, 5, 6, 7], AnimMode.Loop),
        ("fall_away", [5], AnimMode.Pose),
        ("fall", [8, 9, 10, 11], AnimMode.Loop),
        ("inflate", [12], AnimMode.Pose),
        ("run_windup", [22, 23, 24, 25], AnimMode.Loop),
        ("hop_apex", [26, 27], AnimMode.Loop),
        ("fall_start", [28, 29, 30], AnimMode.Once),
        ("turn", [30, 31, 32, 33, 34], AnimMode.Once),
    ];

    // Per world skin present in this install: 00 caves, 02 snow. No "03": Nirpling has no underworld variant.
    private static readonly (string Suffix, string Pal, string[] Shas)[] Reference =
    [
        ("00", "LVL000.PAL",
        [
            "B28BBE3D317ED7ED80C4862E5468763742E9D288D09618B1C8A9CAE494FD1C9A",
            "4172BE429E0C41371CC986641FA8C3A7B3462FA465A392E2D04C9C61427C1AB7",
            "3D9B08715778E2E3B88383F4AF07EE4C14CDF84AC49FE14050F107E8C95883C2",
            "C4EF0E6CB3258704512B351B95BE07FCD0495F3AD6BA7F6044DF3B6383B2F1FF",
            "591DC82385FC54858A4BE47E6F43BA5C8DCC37937B3358DBF3E93F8B9181294D",
            "63005DF9E476D8E16D4405525647B38EB0193EDB028AC562E68E493AE8DBD6A3",
            "F714829A76A5572C6A5EE3F0E36A64DF3C00B71038957FBBCA79EE50991584A7",
            "E9BF6F5F20290FE849A903C639A491229CFB32D504B2DDAAB8B8632026D2D975",
            "42206F78B29F3F39352956672923DC7F248E67F1BBFD277B2624D95C4663F722",
        ]),
        ("02", "LVL400.PAL",
        [
            "CD7E24706113D3C0F9406987789855EB43D24919A8BCD0CDBE3A60635EE505B2",
            "E7819E265244E395C898DF8DC2490EF3C43E9B21EB43576F104F73C3051D17BC",
            "9DAE289BC04D313BBC081865EBC5501535A3A67BFC78D17D4A0ADBBAB8C3ACEC",
            "8092BA9B50DABA730A8C794D9E22FD9AE6CBAF65E9045033C6B1632DB1E71C46",
            "1AEEA0DEC516EF9C5E98031107F85DF47AA7F13EDCDC26F6F12CE537BB17E1CD",
            "0F42B96991A84E75A54B78E4CC2962E812C842E6BCC544C99D39E418E62F119D",
            "ABF2791C26E33FA6CDF363E6B2E9C4449D0ECAA5FE335F72763713522F42CF95",
            "B13CC17EDA64D6EE02578B03CB7AC7C5D3B9087B953ABCBFBC59255248646C23",
            "95F94BC0A198D15616F293F381658211D101D2F4DBF1A7D9186CD6235F22A00E",
        ]),
    ];

    [Fact]
    public void Nirpling_table_matches_reference_transcription()
    {
        var set = AnimationTables.Nirpling;
        Assert.Equal("Nirpling", set.Name);
        Assert.Equal((byte)0x0B, set.Category);
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Fact]
    public void Nirpling_rects_are_not_the_uniform_grid()
    {
        // Against the literal rect formula: (21*i, 150, 20, 17) for i<14, (21*(i-22), 181, 20, 17) for
        // 22<=i<35.
        var rects = AnimationTables.Nirpling.Rects;
        Assert.NotNull(rects);
        Assert.Equal(new FrameRect(0, 0, 150, 20, 17), rects![0]);
        Assert.Equal(new FrameRect(0, 21 * 13, 150, 20, 17), rects[13]);
        Assert.Equal(new FrameRect(0, 0, 181, 20, 17), rects[22]);
        Assert.Equal(new FrameRect(0, 21 * 12, 181, 20, 17), rects[34]);
        // ids 14-21 are the never-drawn spark decorations: genuinely absent, not just unused
        for (int i = 14; i <= 21; i++)
            Assert.False(rects.ContainsKey(i));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Nirpling_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(EnemyGrids.TryGet(0x0B, out var grid)); // sheet-name resolution only; geometry unused here
        var set = AnimationTables.Nirpling;
        var anims = set.Anims;

        // No WO_NRP03 ships at all: a genuine absence, not silently skipped.
        Assert.False(library.Contains($"{grid.Sheet}03.SPF"));
        Assert.False(library.Contains($"{grid.Sheet}03.MPF"));

        foreach (var (suffix, palName, shas) in Reference)
        {
            var pages = SheetImage.Read(library.Read($"{grid.Sheet}{suffix}.SPF")).Frames;
            var palette = VgaPalette.From6Bit(library.Read(palName).AsSpan(0, 768));

            var cells = new Dictionary<int, SpriteCell>();
            foreach (var (frameId, rect) in set.Rects!)
                cells[frameId] = SpriteSheetSlicer.RectCell(pages[rect.Page], rect.X, rect.Y, rect.X + rect.W - 1, rect.Y + rect.H - 1);

            for (int i = 0; i < anims.Count; i++)
            {
                bool absent = anims[i].Frames.All(f => cells[f].IsEmpty);
                Assert.False(absent, $"{suffix}/{anims[i].Name}: expected art, found all-empty cells");

                using var sha = SHA256.Create();
                foreach (var f in anims[i].Frames)
                {
                    var cell = cells[f];
                    var rgba = new byte[cell.W * cell.H * 4];
                    AnimationPixels.ToRgba(cell, palette, rgba);
                    sha.TransformBlock(rgba, 0, rgba.Length, null, 0);
                }
                sha.TransformFinalBlock([], 0, 0);
                Assert.Equal(shas[i], Convert.ToHexString(sha.Hash!), ignoreCase: true);
            }
        }
    }
}
