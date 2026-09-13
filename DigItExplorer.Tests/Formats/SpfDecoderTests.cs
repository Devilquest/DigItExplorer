using System.Security.Cryptography;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="SheetImage"/> against the game's own <c>.SPF</c>/<c>.MPF</c>/<c>.ANI</c> sheets.</summary>
public class SpfDecoderTests
{
    private static readonly string[] SheetExtensions = [".SPF", ".MPF", ".ANI"];

    /// <summary>Decodes every sheet in the copy and guards the totals measured from the build it is.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.MeasuredArchives)]
    public void Decode_all_sheets_reproduces_reference_metrics()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        Assert.True(ArchiveFingerprints.TryIdentify(out var expected));

        using var library = ResourceLibrary.Open(gameDir);

        var names = library.Names
            .Where(n => SheetExtensions.Contains(Path.GetExtension(n), StringComparer.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        // Optional: dump per-frame + per-palette SHA-1 for diffing out of band.
        var manifestPath = Environment.GetEnvironmentVariable("DIGIT_FRAME_MANIFEST");
        using var manifest = manifestPath is null ? null : new StreamWriter(manifestPath, append: false);

        int totalFrames = 0;
        foreach (var name in names)
        {
            var sheet = SheetImage.Read(library.Read(name));

            Assert.Equal(768, sheet.Palette.Rgb.Length);
            Assert.NotEmpty(sheet.Frames);
            manifest?.WriteLine($"{name}\tPAL\t{Sha1(sheet.Palette.Rgb)}");

            for (int i = 0; i < sheet.Frames.Count; i++)
            {
                Assert.Equal(FrameCodec.FrameBytes, sheet.Frames[i].Length);
                manifest?.WriteLine($"{name}\t{i}\t{Sha1(sheet.Frames[i])}");
            }
            totalFrames += sheet.Frames.Count;
        }

        Assert.Equal(expected.Sheets, names.Count);
        Assert.Equal(expected.SheetFrames, totalFrames);
    }

    private static string Sha1(ReadOnlySpan<byte> data) => Convert.ToHexString(SHA1.HashData(data));
}
