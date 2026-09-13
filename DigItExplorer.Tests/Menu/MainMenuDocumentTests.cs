using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Menu;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Guards DLF records and sign placement measurements for the main menu against reference files.</summary>
public class MainMenuDocumentTests
{
    private static ResourceLibrary OpenLibrary()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        return ResourceLibrary.Open(gameDir);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Dlf_header_and_records_match_the_documented_facts()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;
        var dlfBytes = loader("LVL900.DLF");
        Assert.NotNull(dlfBytes);

        var header = DlfHeader.Read(dlfBytes);
        Assert.Equal(7, header.Count);
        Assert.Equal(640, header.Width);
        Assert.Equal(200, header.Height);
        Assert.Equal(90, header.ParaSet);

        var records = DlfRecord.ReadAll(dlfBytes);
        Assert.Equal(8, records.Count);
        Assert.Equal(5, records.Count(r => r.Category == 0x06));
        Assert.Equal(3, records.Count(r => r.Category == 0x07));
        Assert.All(records, r => Assert.True(r.Category is 0x06 or 0x07));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Load_composes_the_map_background_and_every_shipped_sign()
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;

        var doc = MainMenuDocument.Load(loader);
        Assert.NotNull(doc);
        Assert.Equal(640, doc!.Map.Terrain.Width);
        Assert.Equal(200, doc.Map.Terrain.Height);
        Assert.Equal(640 * 200, doc.BackgroundIndices.Length);

        foreach (var file in new[] { "MENU01.MPF", "MENU02.MPF", "MENU03.MPF", "MENU04.MPF" })
        {
            Assert.True(loader(file) is not null, $"{file} is missing from this install");
            Assert.True(doc.Signs.ContainsKey(file), $"{file} ships but the document composed no sign for it");
        }
    }

    /// <summary>Verifies horizontal anchor offsets and transparent color keys for menu sign files.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameTheory]
    [InlineData("MENU01.MPF", 0, (byte)8)]
    [InlineData("MENU02.MPF", 320, (byte)112)]
    [InlineData("MENU03.MPF", 320, (byte)112)]
    [InlineData("MENU04.MPF", 320, (byte)112)]
    public void Sign_placements_match_the_documented_measurement(string file, int expectedAnchor, byte expectedColorKey)
    {
        using var library = OpenLibrary();
        var loader = library.TryRead;
        Assert.NotNull(loader(file));

        var doc = MainMenuDocument.Load(loader);
        Assert.NotNull(doc);
        Assert.True(doc!.Placements.TryGetValue(file, out var placement), $"{file} failed its margin check.");
        Assert.Equal(expectedAnchor, placement.Anchor);
        Assert.Equal(expectedColorKey, placement.ColorKey);
    }
}
