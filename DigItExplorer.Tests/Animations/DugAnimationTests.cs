using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Dug animation tables and rendered frame hashes against reference files.</summary>
public class DugAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("skid", [0, 1, 0, 1, 0, 1, 2, 3], AnimMode.Once),
        ("run_start", [4, 5, 6, 7, 8, 9], AnimMode.Once),
        ("run", [10, 11, 12, 13, 14, 15, 16, 17, 18, 19], AnimMode.Loop),
        ("jump_takeoff", [20, 20, 21, 22], AnimMode.Once),
        ("jump_rise", [23, 24, 25, 26, 27, 28], AnimMode.Once),
        ("fall", [29], AnimMode.Pose),
        ("land", [30, 31, 31], AnimMode.Once),
        ("jump_full", [20, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 29, 29, 29, 29, 29, 30, 31, 31], AnimMode.Once),
        ("idle", [32, 33], AnimMode.Loop),
        ("idle_turn", [34, 35, 34, 35, 36, 37, 38], AnimMode.Once),
        ("push_wall", [34, 35], AnimMode.Loop),
        ("rope_climb", [44, 45, 46, 47, 48, 49], AnimMode.Loop),
    ];

    // Per costume digit: sheet, palette, and one SHA-256 per animation over its concatenated RGBA frames in
    // table order. A null hash means that animation is absent from that costume's sheet.
    private static readonly (char Code, string Sheet, string Pal, string?[] Shas)[] Reference =
    [
        ('0', "DUG0A.SPF", "LVL000.PAL",
        [
            "8288697D49B32A80B35B8C5D47FA5448E4E438C72B8B23EDAEC0412FFDD77437",
            "B80B488B9B1B929A72FB8695DB296668C9BB7963BE3B70346EE314213FDA92F2",
            "F227397B58A47422FFCBE68E6E0E02E0E2E45F197463EF24F68FBE5FF22C1141",
            "FC14133E9CC1363F9306F552172778A6EA5CF3841B9104A941EF62EF055CB7D5",
            "E76983EFB90D490EF7A36F36B048390E9F12F010786ED81420E03AF3E32D3EBA",
            "84200394A1C9B548E3F81B18D99C19895C47ADB37572E10F85F9F20E5306EFFC",
            "BFE4B935B7A1459B276BF15E60FAE0088066A017D9A510C3CBAF13AC9E5CB79F",
            "A677D81817B8D0524518DDDF25CEF88183E38694896B0080D7953B960B07D48F",
            "83A77664E2847B8B5493E493DDC16404D7EBBF463BF0C9BEC03E5C0C2F9EEEE5",
            "2F82858982028BBE5B2B921A74663015EE79364CB564C456769A1E51AE22FA44",
            "FCCAEE368F210B0AF133463B2C4C46E63915EC4984B515B3CA293A4C8EC05AD8",
            "BA93CC203E1330A634F64FDBAC5E43234FD8A5E6F8E2C4EE710D2BD8A9194195",
        ]),
        ('6', "DUG6A.SPF", "LVL400.PAL",
        [
            "1972B3B8A76BA2E38FE4B76A5B9E7F13061AD86AF6F83A982B50FCDE0018A865",
            "73AEBB0AC6C9232E3159FEA4B4668407EB47D6AAB8458EACB1C67E0FCB4A47E3",
            "D60362931102924EAEEBD0E67F70A7D569F0EC24D2126BC7BD5EA314335F85C2",
            "CB7E0E4DF0BFE126619F021EF5D47A1B9F22059D98598D3C0EBAB9554CA1F725",
            "ACFE6ECF2537F548BD81B95BF551FA1474ED62A0B84DC148843B03CC60A2B35A",
            "C6C6B03F29E4A8CBCDC66F11DFC6F2F632E68A5D69F3FA0E1B55C77DAF46058A",
            "33940F29AFAC44384DE31176B1C6DF5ACD5F98931C8DB755853B84BE4669A21F",
            "7B95F3D3C38F4AD6DD7E7AF791399D4AE1182E1C12F8473AE9406E3184C56ED8",
            "544D07D87476ADEB3D822D5032E087FA9172C3C48AAA6D318F6A25F99B2E1AA5",
            "F9FCA2AD674D33F37E9F33AED3E71E29AF58707D65B9CE196758A4277AD75D89",
            "7FD24D93679B61BD6B9A37B2D64A595009BFA7DE791236E28748BF789AEF0D91",
            "4E3A2E638F2F2DB2264D7F69160E22200C6CE481E884120EC56A1B044CA164E1",
        ]),
        ('3', "DUG3A.SPF", "LVL600.PAL",
        [
            "DFEC71CC23BECACA6F129C2686786ACC9E4EFDC15CC32D7783C0803B1D328098",
            "75DDDC1E43D3408E2249A7E78722FF64C665C65E2CCE5AA64837ABACBCD792C0",
            "FAAE30759AC205BD339F18C779FD1638E23A06395AE373F5458A9F296EA69759",
            "D4F6926C15D02C521C7CE8FC0BEF48B52F5516A32BD8F46EC37B196323E73E66",
            "1F822315F89643D4B341CB572238C262200386E5537ED3CE93F8AE37F5A69193",
            "DC54898ABA10C50D8E4BA63E71C16AAB3C2403381B9AA6EF7E9C5FCCB5E306A9",
            "BE1B085B7C9E4FB7D8E4EFF95255D3CC76DC8ED711CECCA8A45D090C7F7976D6",
            "75F5AF851C756D4365911AB24A0EDA498F5A7EF60A7EDA3DEBE9DE41CE6EAB01",
            "C9C7F71ED8615FA1A6D8B0510C291ACBADCB8735461E4F003448BBB2CD9897F5",
            "D4BB5AB3FE2FF00B3E3418F7C87B4C37B30C9C638C13F67771955852F75D2990",
            "79313C18151B31EE3C704BA37C4FA243FFC66C7003233F06861E5E42DD882448",
            "541769AF604EB71603C01DE05DFA0FD206C01548E51AD4274618409B45AE258D",
        ]),
        ('5', "DUG5A.SPF", "LVL000.PAL",
        [
            null, // skid: absent from this costume's sheet
            "A392A9F7BE60405A5C6D102D6E23FD347F33F74D0E3287A2EF05AF2CB448283E",
            "C4C307B662CE762301676E9A0F7685AAE611CB2A6BF5EA7BB7F9C0940FD05B34",
            "2C78796E6B8C327E69F2AA24D1BB57DB8899E629181BC72E7206238B9609F6AA",
            "F41383322EC9B4EEBE55D7E557B2D53C215C827BCE11B57104F87607377B9942",
            "11F911865875B61D7BF4E0649EADBFAD2636EC07557ECB0EBA4AE7181F1C572D",
            "F3CB0F983CA6B1B58369A6E1AF72CEE74DA61F19522FA84DDDB0A9F53BCD8277",
            "2BAE95EA7F759486C5428CA1D973B56BBC1C018D36BF9C93C528F9390965CBC0",
            "1A4CD1AE4E87B3923BEA49F7D3C920609DD150A63E49A1DB2F2A0CC1D5002134",
            "F2B0B1FEEF9751D2A0454FAA744BC033CFFF4F2132BC5D9E5463D7E0E0438B83",
            "E08A5D195C9D9148B20B45C50EA2F596D0E02A8E518BBC1FC2C76E78C2E40C85",
            "55920EF8313952E7184D5A7589DB66E3224A7A59E88EA008A39F48722322A344",
        ]),
    ];

    [Fact]
    public void Dug_table_matches_transcription_in_ascending_frame_order()
    {
        var set = AnimationTables.Dug;
        Assert.Equal("Dug", set.Name);
        Assert.Null(set.Category); // not a DLF-instance enemy: DGROUP globals only, keyed by SheetLetter instead
        Assert.Equal('A', set.SheetLetter);
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Fact]
    public void Rects_are_the_literal_DugAbGrid_formula()
    {
        Assert.Equal(new FrameRect(0, 0, 1, 32, 36), AnimationTables.Dug.Rects![0]);
        Assert.Equal(new FrameRect(0, 9 * 32, 1, 32, 36), AnimationTables.Dug.Rects[9]);   // i=9 -> col 9, row 0
        Assert.Equal(new FrameRect(0, 0, 37 + 1, 32, 36), AnimationTables.Dug.Rects[10]);  // i=10 -> col 0, row 1
        Assert.Equal(new FrameRect(0, 4 * 32, 4 * 37 + 1, 32, 36), AnimationTables.Dug.Rects[44]); // i=44 -> col 4, row 4
        Assert.Equal(new FrameRect(0, 9 * 32, 4 * 37 + 1, 32, 36), AnimationTables.Dug.Rects[49]); // i=49 -> col 9, row 4
        for (int i = 0; i < 50; i++)
            Assert.True(AnimationTables.Dug.Rects.ContainsKey(i));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Dug_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var set = AnimationTables.Dug;

        foreach (var (code, sheetFile, palName, shas) in Reference)
        {
            var pages = SheetImage.Read(library.Read(sheetFile)).Frames;
            var palette = VgaPalette.From6Bit(library.Read(palName).AsSpan(0, 768));

            var cells = new Dictionary<int, SpriteCell>();
            foreach (var (frameId, rect) in set.Rects!)
                cells[frameId] = SpriteSheetSlicer.RectCell(pages[rect.Page], rect.X, rect.Y, rect.X + rect.W - 1, rect.Y + rect.H - 1);

            for (int i = 0; i < set.Anims.Count; i++)
            {
                bool absent = set.Anims[i].Frames.All(f => cells[f].IsEmpty);
                if (shas[i] is null)
                {
                    Assert.True(absent, $"costume {code}/{set.Anims[i].Name}: expected absent, has art");
                    continue;
                }
                Assert.False(absent, $"costume {code}/{set.Anims[i].Name}: expected art, found all-empty cells");

                using var sha = SHA256.Create();
                foreach (var f in set.Anims[i].Frames)
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
