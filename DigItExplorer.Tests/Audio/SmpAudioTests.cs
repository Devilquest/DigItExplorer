using DigItExplorer.Core.Archives;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="SmpAudio"/> against the game's own <c>.SMP</c> sound effects.</summary>
public class SmpAudioTests
{
    [Fact]
    public void ToPcm16_maps_signed_bytes_to_little_endian_shorts()
    {
        // 0x00 = silence, 0x7F = max positive, 0x80 = max negative, 0xFF = -1.
        byte[] raw = [0x00, 0x7F, 0x80, 0xFF];
        var pcm = SmpAudio.ToPcm16(raw);

        Assert.Equal(8, pcm.Length);
        short[] expected = [0, 0x7F00, -32768, -256];
        for (int i = 0; i < expected.Length; i++)
            Assert.Equal(expected[i], (short)(pcm[i * 2] | pcm[i * 2 + 1] << 8));
    }

    [Fact]
    public void DurationOf_uses_one_byte_per_sample_at_11025()
    {
        Assert.Equal(1.0, SmpAudio.DurationOf(11025).TotalSeconds, 6);
        Assert.Equal(0.0, SmpAudio.DurationOf(0).TotalSeconds, 6);
    }

    /// <summary>Verifies that game sound effects decode to 11025 Hz signed PCM with documented durations.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Game_smp_files_decode_with_documented_shape()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        var effects = new List<(string Name, byte[] Data)>();
        foreach (var archivePath in Directory.EnumerateFiles(gameDir, "*.XRS"))
        {
            using var archive = XrsArchive.Open(archivePath);
            foreach (var entry in archive.Entries)
                if (entry.Name.EndsWith(".SMP", StringComparison.OrdinalIgnoreCase) &&
                    !effects.Any(e => e.Name.Equals(entry.Name, StringComparison.OrdinalIgnoreCase)))
                    effects.Add((entry.Name, archive.Read(entry)));
        }

        Assert.Equal(19, effects.Count);
        foreach (var (name, data) in effects)
        {
            var seconds = SmpAudio.DurationOf(data.Length).TotalSeconds;
            Assert.True(seconds is >= 0.10 and <= 1.14, $"{name}: {seconds:F2}s outside documented range");

            short first = (short)((sbyte)data[0] << 8);
            Assert.True(Math.Abs(first) < 0x2000, $"{name}: does not open near signed silence");

            Assert.Equal(data.Length * 2, SmpAudio.ToPcm16(data).Length);
        }
    }
}
