using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Draggo animation tables and rendered frame hashes across all variants against reference files.</summary>
public class DraggoAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("walk", [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], AnimMode.Loop),
        ("turn", [10, 11, 12, 13, 14], AnimMode.Once),
        ("inflate", [15, 16, 17, 18, 19, 20, 21, 22, 23], AnimMode.Once),
        ("deflate", [23, 22, 21, 20, 19, 18, 17, 16, 15], AnimMode.Once),
        ("fall_away", [24], AnimMode.Pose),
        ("scratch_head",
         [25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35,
          29, 30, 31, 32, 33, 34, 35,
          29, 30, 31, 32, 33, 34, 35,
          36, 37, 38], AnimMode.Once),
    ];

    // Per world skin: sheet suffix, palette, and one SHA-256 per animation over its concatenated RGBA frames
    // in table order. A null hash means that animation is absent from that skin.
    private static readonly (string Suffix, string Pal, string?[] Shas)[] Reference =
    [
        ("00", "LVL000.PAL",
        [
            "E03C2900CA4EC702C8CA41FD855BA714331E4E4C642D99FE248726B22999FC37",
            "CEECCA82C92824FEC1B24B4B132F87471D526D3D90B6BF5698C68784F45657B3",
            "DC7CF919FB24E7A8315BC93DB7C89D76182A0501E7E4C1BE9C0A372F7768CD8A",
            "86E1A48291B8CD3F20E381ABD30F2329899F9AEB88D068A97D383069BD33DD63",
            "7B618B928A23F33E08F02C6F55EEEF14488361436A31CEFD574FCAF4E01ADA86",
            "66EDB36C829566BB82EE1C431D16F133E14A4AA45160BFFF6E218B19A285FBF6",
        ]),
        ("02", "LVL400.PAL",
        [
            "E777975D8B1DAD3F53C699B99FC3F110F2D3062AB99B84BF525C984EE7E39553",
            "8EAD57A7E956A7CC0BF05383D32E2D77506BD94150882C471EC871CCEDF704A8",
            "8E31C5EA995FB958627534475A3E92DEE3959B8C03E39F61B0E997E583FF6270",
            "A68B248F17EC18C961EB25C4DFAB55DBE546828B683A6485C066413ED416DAFA",
            "E358E85FB411B4EEE1753BF670C5E945B2EBCBDEA9C2BEE8640B3D566E2E4FA6",
            "00C0214F98FFF0AABCDF7C0843788B3B6ED60CB11F78A3379F969ECD782F5D8B",
        ]),
        ("03", "LVL600.PAL",
        [
            "5B97641FF941AD06DBE678722F6C9B024776DE67B651EE9CD223F75AFD4C7990",
            "00AD50EE5813FC9FEC53ECCC0C60082F31712D5A7D929BF541D95BD2302ECCE6",
            "716A6D9724C9F0A8E3BD39347FD79378379C13797E2FE58E6F0DD1FC23D39E1D",
            "1AF4A966E04782DF49E16D3C7CF982FD7036DC3CF80DEB6467543DF503A0325A",
            "19D731E65E95E014233DC937874185D03C047FC0D10A3380E5E2B01F00814B60",
            null, // scratch_head: absent from this skin
        ]),
    ];

    [Fact]
    public void Draggo_table_matches_reference_transcription()
    {
        var set = AnimationTables.Draggo;
        Assert.Equal("Draggo", set.Name);
        Assert.Equal((byte)0x07, set.Category);
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Draggo_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        Assert.True(EnemyGrids.TryGet(0x07, out var grid));
        var anims = AnimationTables.Draggo.Anims;

        foreach (var (suffix, palName, shas) in Reference)
        {
            var pages = SheetImage.Read(library.Read($"{grid.Sheet}{suffix}.SPF")).Frames;
            var palette = VgaPalette.From6Bit(library.Read(palName).AsSpan(0, 768));

            var cells = new SpriteCell[39];
            for (int f = 0; f < cells.Length; f++)
                cells[f] = SpriteSheetSlicer.GridCell(pages, grid.Cols, grid.StrideX, grid.StrideY, grid.W, grid.H, f);

            for (int i = 0; i < anims.Count; i++)
            {
                bool absent = anims[i].Frames.All(f => cells[f].IsEmpty);
                if (shas[i] is null)
                {
                    Assert.True(absent, $"{suffix}/{anims[i].Name}: expected absent, has art");
                    continue;
                }
                Assert.False(absent, $"{suffix}/{anims[i].Name}: expected art, found all-empty cells");

                using var sha = SHA256.Create();
                var rgba = new byte[grid.W * grid.H * 4];
                foreach (var f in anims[i].Frames)
                {
                    AnimationPixels.ToRgba(cells[f], palette, rgba);
                    sha.TransformBlock(rgba, 0, rgba.Length, null, 0);
                }
                sha.TransformFinalBlock([], 0, 0);
                Assert.Equal(shas[i], Convert.ToHexString(sha.Hash!), ignoreCase: true);
            }
        }
    }
}
