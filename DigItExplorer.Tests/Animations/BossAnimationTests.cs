using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;
using DigItExplorer.Core.Knowledge.Animations;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Pins Boss animation tables and rendered frame hashes against reference files.</summary>
public class BossAnimationTests
{
    private static readonly (string Name, int[] Frames, AnimMode Mode)[] ExpectedTable =
    [
        ("walk", [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], AnimMode.Loop),
        ("turn", [10, 11, 12, 13, 14], AnimMode.Once),
        ("tantrum", [15, 16, 17, 18, 19, 20, 21, 22, 23, 15, 16, 17, 18, 19, 20, 21, 22, 23, 15, 16, 17, 18, 19, 20, 21, 22, 23], AnimMode.Once),
        ("leap", [16, 17, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 21, 22, 23], AnimMode.Once),
        ("delay", [23], AnimMode.Pose),
        ("fall", [23], AnimMode.Pose),
        // The three runs of 29 are in the game's own script, not a transcription slip.
        ("spit", [24, 25, 26, 27, 28, 29, 29, 29, 28, 27, 26, 26, 26, 26, 27, 28, 29, 29, 29,
                  28, 27, 26, 26, 26, 26, 27, 28, 29, 29, 29, 28, 27, 26, 25, 24], AnimMode.Once),
        ("die", [30, 31, 32, 33, 34, 35], AnimMode.Once),
    ];

    // One SHA-256 per animation over its concatenated RGBA frames, in the table order above.
    private static readonly string[] Shas =
    [
        "1E765AC04A09BB26E059DEE2B7CABA59E38413E9EBCC2E392AF7A07CA3F99D5C", // walk
        "111CBEE11AD2F4AA301C4C6A562205BFDA64B3DC169EF78885963402653208AE", // turn
        "20401FD30D35017C27A4D566D609E2C0802C070885B6C1730CC51548D7F38B80", // tantrum
        "756A6C629F324825672015E8C427DC32F3D1574F9C6C72C7D7453F1FD7780AC5", // leap
        "86AB8C54C5C4D87C4B87925745BD15AA17A7762E36053A853BCE4DAFEA42DDF5", // delay
        "86AB8C54C5C4D87C4B87925745BD15AA17A7762E36053A853BCE4DAFEA42DDF5", // fall (same fixed pose as delay)
        "82AFEB3C7399B71CAA5CBBB1B2B899023E45F08B291A192A0A1E34F00D24DA1B", // spit
        "9FE7AF38376F1779C0DFE51CEDB1D274B9977BBBA538564803B0F008A935EDBF", // die
    ];

    [Fact]
    public void Boss_table_matches_reference_transcription()
    {
        var set = AnimationTables.Boss;
        Assert.Equal("Supreme Spurkasaur", set.Name);
        Assert.Equal((byte)0x32, set.Category);
        Assert.Equal("WO_BOSS", set.FixedSheet);
        Assert.Equal("LVL600.PAL", set.FixedPalette);
        Assert.Equal(World.Boss, set.FixedLocationWorld); // labeled with its own arena's world name
        AnimationGuard.AssertTable(set, ExpectedTable);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void Script_tables_match_the_live_data_segment_bytes()
    {
        // The three script-driven animations are long and repetitive, so they are checked against the
        // game's own data-segment words rather than against a second copy of the same list. The copy above
        // and the table it guards are both transcriptions; only these bytes are the source they came from.
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        var exe = GameExecutable.Open(gameDir, "MAIN.EXE");
        Assert.True(BuildLayout.TryRecognize(exe, out var layout),
            "the install's MAIN.EXE is not a build whose addresses this application knows");

        var reader = new ExeReader(exe, layout);
        var anims = AnimationTables.Boss.Anims;

        Assert.Equal(anims.First(a => a.Name == "tantrum").Frames, ReadWords(reader, 0xCFA, 27));
        Assert.Equal(anims.First(a => a.Name == "spit").Frames, ReadWords(reader, 0xD30, 35));
        Assert.Equal(anims.First(a => a.Name == "leap").Frames, ReadWords(reader, 0xDBC, 17));
    }

    // The script tables are globals, so their offsets are relative to DGROUP rather than to the file.
    private static int[] ReadWords(ExeReader reader, int offset, int count)
    {
        var words = new int[count];
        for (int i = 0; i < count; i++)
            Assert.True(reader.TryReadWord(ExeLayout.DGroup(offset + i * 2), out words[i]));
        return words;
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Boss_frame_pixels_match_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        var set = AnimationTables.Boss;
        var pages = SheetImage.Read(library.Read("WO_BOSS.MPF")).Frames;
        var palette = VgaPalette.From6Bit(library.Read("LVL600.PAL").AsSpan(0, 768));

        AnimationGuard.AssertRectHashes(set, pages, palette, Shas);
    }
}
