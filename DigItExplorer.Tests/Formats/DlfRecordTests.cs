using System.Buffers.Binary;
using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="DlfRecord"/> against the game's own <c>.DLF</c> level-object records.</summary>
public class DlfRecordTests
{
    /// <summary>Guards round-trip serialization and signed coordinate parsing for DLF records across sample levels.</summary>
    private static readonly (string Level, int Count, string Sha)[] Reference =
    [
        ("LVL000", 78, "14030CE8248EF7C71351C8120609DB2C09B1D86B8BBBBF8645A0211BEBFFAD37"),
        ("LVL209", 10, "5B96A42D2BAC9228D686E0705ACC0D742810AB58D297AEF20CBACD87F6ECF5F1"),
        ("LVL630", 14, "0B1FF0A944D560F343A97B79F8B89E32F357A152457EDAF37DB525FDD508F57F"),
        ("LVL750", 2, "113CF7A2663272B68780E9D893B44E539A2337E8BA7EF5E2FA7DB239B59817B0"),
        ("LVL900", 8, "C2EA6DA6A094706C0F17D0F2FBA1A07D79CC5B09416FC1BEF10E59E329BFAD52"),
    ];

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Record_parsing_matches_reference_pixels()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        using var library = ResourceLibrary.Open(gameDir);

        foreach (var (level, count, expected) in Reference)
        {
            var dlf = $"{level}.DLF";
            Assert.True(library.Contains(dlf), $"{level}.DLF missing in {gameDir}");

            var records = DlfRecord.ReadAll(library.Read(dlf));
            Assert.Equal(count, records.Count);

            var buf = new byte[records.Count * 16];
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                var span = buf.AsSpan(i * 16, 16);
                span[0] = r.Category;
                span[1] = r.Type;
                BinaryPrimitives.WriteInt16LittleEndian(span[2..], r.X);
                BinaryPrimitives.WriteInt16LittleEndian(span[4..], r.Y);
                BinaryPrimitives.WriteUInt16LittleEndian(span[6..], r.P0);
                BinaryPrimitives.WriteUInt16LittleEndian(span[8..], r.P1);
                BinaryPrimitives.WriteUInt16LittleEndian(span[10..], r.P2);
                BinaryPrimitives.WriteUInt16LittleEndian(span[12..], r.P3);
                BinaryPrimitives.WriteUInt16LittleEndian(span[14..], r.P4);
            }

            var actual = Convert.ToHexString(SHA256.HashData(buf));
            Assert.Equal(expected, actual, ignoreCase: true);
        }
    }
}
