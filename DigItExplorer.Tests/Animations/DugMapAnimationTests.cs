using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins world map player walking and standing animation tables and frame hashes against reference files.</summary>
public class DugMapAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("map_walk_right", [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], AnimMode.Loop),
        ("map_walk_left", [10, 11, 12, 13, 14, 15, 16, 17, 18, 19], AnimMode.Loop),
        ("map_walk_down", [20, 21, 22, 23, 24, 25, 26, 27], AnimMode.Loop),
        ("map_walk_up", [30, 31, 32, 33, 34, 35, 36, 37], AnimMode.Loop),
        ("map_stand_right", [40], AnimMode.Pose),
        ("map_stand_left", [41], AnimMode.Pose),
        ("map_stand_front", [42], AnimMode.Pose),
        ("map_stand_back", [43], AnimMode.Pose),
    ];

    private static readonly (char Code, string Sheet, string Pal, string[] Shas)[] Reference =
    [
        ('0', "DUG0D.SPF", "LVL000.PAL",
        [
            "558978F5498DC3AFA7BD41B4925AF274A4C2CEDD88F28E94FF4A4E44B27D21AA",
            "A5DA645518AE6C678BB561BBF160E08C4ADF313A06EDC5B040ADF3B37CE303E5",
            "71334923FB482E2AE49C93EDA46F0325425320A4AE8720EBE28373983581E722",
            "038DF0C73629AEEA4DC1CF94627E3B73E0FFE2BA9EC8B1C8F53731DCD4450CF7",
            "D418048C916835873191F9BFA8B456B92F5976652C2234D254BC2CF133748F91",
            "0D675E482012ADCE276B9D5D201651C4B4F1C24F31C207D175302C0F0E4A79D9",
            "77702FA694855BC93C47BE2FEC7FB8375165FDF291687DC0E171261D438133B6",
            "25D4828AF96A8625AFD44C5B7CBCB623D645EEB206A1E277626BB8025853BE3A",
        ]),
        ('6', "DUG6D.SPF", "LVL400.PAL",
        [
            "61B38EE50CBEC51729290ABADFE2A379ACC3C61BCC9BFEF67965CE3F1EC372F6",
            "5F1FFEF25595E8829CAED740DC77E29729CEDF65F063EA34C02233A86D5F13FC",
            "9235A278F833A5CE6BC078A9B004B1E3FAF0DC08E8C6B186D02406032F77F91C",
            "9BF1700B65DB6D29C46760B7550E74BA2443EF1EDE6B31B5DB5B76B7BE396405",
            "29C986D058A51D4D32CACF1212B116D26A250F11AEC148D083C3735BCF7EB921",
            "44C8FEC0AD5B371886114C21DF815030CD5B82431B29B8FE8E29CA7AB993AA2A",
            "90DACB41BF9FD34F98EE344C295D05CD24D7F3C18C58A04B2A726094E87D60B7",
            "7A5B85F947D08B8B4289AC58780121FC6231A34A8A3C212B42491C9A31634CCC",
        ]),
        ('3', "DUG3D.SPF", "LVL600.PAL",
        [
            "55BA58EA339A864481C1F7C9DF79D85AB6E2FEEB0322EF9A4F97976BD67B9704",
            "BD85EE63F3691D0300049CE952FF0766777157107A54C1EDEC13F04055368484",
            "0E6CFC4CF9448CCC7E3F8B1F3D43D6498B6A7C6D29E5049806902D2ECDC84A07",
            "C96C92D10EFF3F1F5B07C301529B6ACA20E466EDAEF64B38812988F35303F516",
            "62F88DD8E22EB01ACB1172F29EECD023BB656F42FB075A3A71237507AB283F21",
            "F55DA62895B2E20435473D71E3C60FA330526B027DD438DBE95AC6CFD9D46350",
            "5D0CF3E2F73105828DF8FAF3C2A1A8876B6B3FF572A9C42C60C6E18F02A641EC",
            "05285B33B982531F82771A288D6BC2B45ED43AC23860FE943895C3614A97CE57",
        ]),
        ('5', "DUG5D.SPF", "LVL000.PAL",
        [
            "61A169205BEA2C28D38206B6C2F71843720FED42AA9960E7D5609AA2D8CC5EE5",
            "7B1559EFA6CCF615B1F52F24843A87A19D142D29589F30A78B627911E783053D",
            "66A24CE733EB9FA7DBB5BCCD066BE9FF486C89A38285FC801823B2A9CAB83E79",
            "7E0EE2D26A45FAABD93B72BC142AE8A1AF60F5507537E05EF37E80BA758F4660",
            "322D28DAC9E29A6063427A7A64B7052A36760AC90EAAF35B0E34049E86D1AED3",
            "5D76A96959DA53A13E7D1C18C7EEBA7D552B5F5EC3B54AFF939B6DC757253BE5",
            "08CB7627B9A9148398CEEF4176F40B70E1265BEFB0A45933E2C9127184448A41",
            "23BD5D18B7529FED9B89DB3F3EDA7E60CC8ABEF0C148E7BFDB5945EAFAE138F2",
        ]),
    ];

    [Fact]
    public void DugMap_table_matches_transcription()
    {
        var set = AnimationTables.DugMap;
        Assert.Equal("DugMap", set.Name);
        Assert.Null(set.Category);
        Assert.Equal('D', set.SheetLetter);
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void DugMap_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var set = AnimationTables.DugMap;
        foreach (var (code, sheetFile, palName, shas) in Reference)
        {
            var pages = SheetImage.Read(library.Read(sheetFile)).Frames;
            var palette = VgaPalette.From6Bit(library.Read(palName).AsSpan(0, 768));

            AnimationGuard.AssertRectHashes(set, pages, palette, shas);
        }
    }
}
