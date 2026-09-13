using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Dug digging animation tables and rendered frame hashes against reference files.</summary>
public class DugDigAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("dig_windup", [100, 101, 102, 103, 104], AnimMode.Once),
        ("dig_drill", [105, 106, 107, 108], AnimMode.Loop),
        ("dig_fail", [105, 106, 107, 108, 105, 106, 107, 108, 105, 106, 107, 108,
                       109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123,
                       116, 117, 118, 119, 120, 121, 122, 123,
                       124, 125, 126, 127], AnimMode.Once),
    ];

    private static readonly (char Code, string Sheet, string Pal, string[] Shas)[] Reference =
    [
        ('0', "DUG0C.SPF", "LVL000.PAL",
        [
            "6DCF3C3C5D64A6F0DCC895FF5EB3582AE641975CC9213AF485458F7AF77B4133",
            "32B3A73954675B3BDAF7150BF3B9E81061DDA9C74720CA368C771EA374FB0B98",
            "9F944010630871F19089F9C0A088A90B0B8B7EB8521957B5D88325FD6767E123",
        ]),
        ('6', "DUG6C.SPF", "LVL400.PAL",
        [
            "FF04588F445F1E026EA7E40343DC56EAB296E904AA62B15A5363C9836E5BD668",
            "5CBED1B4FF4BFD03E2987424029CDBE62AFD171BFC804E1A7950EEFABE5B47E2",
            "0B59D60BB28E36FFDF8E5CFD34DBB5985B97126CADFDB743F311289CCEFA9CFB",
        ]),
        ('3', "DUG3C.SPF", "LVL600.PAL",
        [
            "4D69A90806579F850709BD20AC81516A6CF0B5F71F623081829C9F594DF195C6",
            "58DEF0E0CCCB61798A563EA23D110390440B15B997DA7D3272DC785715F255C3",
            "C4BD9D289763D9F8C04B548D3C8FFA3C2A4279C55223A05E603FAE3B6F1D19A0",
        ]),
        ('5', "DUG5C.SPF", "LVL000.PAL",
        [
            "D2898B0A6215DA12AC0826001971F7AC1C006F69ED552E411698FBC5DE00A5CB",
            "73323F1A154716EFA837FC8B3A7DCFDE6EE3F5743D3B710F6AE46E44086AAF3F",
            "BC978D4714E013F1060084FE0B12602576202DFF309B8DD4E2F5C17BAF1CE208",
        ]),
    ];

    [Fact]
    public void DugDig_table_matches_transcription()
    {
        var set = AnimationTables.DugDig;
        Assert.Equal("DugDig", set.Name);
        Assert.Null(set.Category);
        Assert.Equal('C', set.SheetLetter);
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void DugDig_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var set = AnimationTables.DugDig;
        foreach (var (code, sheetFile, palName, shas) in Reference)
        {
            var pages = SheetImage.Read(library.Read(sheetFile)).Frames;
            var palette = VgaPalette.From6Bit(library.Read(palName).AsSpan(0, 768));

            AnimationGuard.AssertRectHashes(set, pages, palette, shas);
        }
    }
}
