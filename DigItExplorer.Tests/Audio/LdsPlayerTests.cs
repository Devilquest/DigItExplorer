using DigItExplorer.Core.Archives;
using DigItExplorer.Core.Audio;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="LdsPlayer"/>/<see cref="LdsRenderer"/>/<see cref="LdsSynthesizer"/> against the
/// game's own LOUDNESS Player music files.</summary>
public class LdsPlayerTests
{
    /// <summary>Guards per-track duration and loop detection metrics against reference LDS music files.</summary>
    private static readonly (string Name, double Seconds, bool Loops)[] DocumentedTracks =
    [
        ("J_DIED.DAT", 4.3, false), ("J_PIXEL.DAT", 4.2, false), ("J_LOUD.DAT", 5.5, false),
        ("J_GOVER2.DAT", 9.1, false), ("J_WIN2.DAT", 11.8, false), ("J_LONG.DAT", 13.8, false),
        ("J_WIN.DAT", 16.2, false), ("J_SAD2.DAT", 16.5, false), ("J_SAD.DAT", 16.6, false),
        ("J_GOVER.DAT", 18.6, false),
        ("J_SUPER.DAT", 12.5, true), ("J_LOOP3.DAT", 22.1, true), ("J_BONUS.DAT", 23.4, true),
        ("J_SUPERM.DAT", 27.3, true), ("J_LOOP2.DAT", 51.5, true),
        ("J_LOOP.DAT", 9.1, true), // the loop-fix case
        ("MAIN.DAT", 129, true), ("TUNE3.DAT", 147, true), ("TUNE7.DAT", 160, true),
        ("TUNE2.DAT", 164, true), ("TUNE6.DAT", 174, true), ("TUNE4.DAT", 188, true),
        ("TUNE5.DAT", 196, true), ("TUNE0.DAT", 213, true), ("TUNE8.DAT", 232, true),
        ("TUNE1.DAT", 235, true), ("TUNE9.DAT", 290, true),
    ];

    private static Dictionary<string, byte[]> ReadAllDats()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        var dats = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var archivePath in Directory.EnumerateFiles(gameDir, "*.XRS"))
        {
            using var archive = XrsArchive.Open(archivePath);
            foreach (var entry in archive.Entries)
                if (entry.Name.EndsWith(".DAT", StringComparison.OrdinalIgnoreCase))
                    dats.TryAdd(entry.Name, archive.Read(entry));
        }
        return dats;
    }

    /// <summary>All 32 music files parse; GPALFIX.DAT (not music) is rejected.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Game_dat_files_parse_as_documented()
    {
        var dats = ReadAllDats();

        int music = 0;
        foreach (var (name, data) in dats)
        {
            bool ok = LdsPlayer.TryLoad(data, out _);
            if (name.Equals("GPALFIX.DAT", StringComparison.OrdinalIgnoreCase))
                Assert.False(ok, "GPALFIX.DAT is not music and must not parse");
            else
            {
                Assert.True(ok, $"{name}: failed to parse as LDS");
                music++;
            }
        }
        Assert.Equal(32, music);
    }

    /// <summary>Verifies that simulated track durations and loop flags match documented reference values.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Simulated_durations_match_documented_table()
    {
        var dats = ReadAllDats();

        foreach (var (name, seconds, loops) in DocumentedTracks)
        {
            Assert.True(dats.TryGetValue(name, out var data), $"{name} missing from install");
            Assert.True(LdsRenderer.TryProbe(data!, out var duration, out var actualLoops), $"{name}: probe failed");

            // documented values are rounded to 0.1 s (or whole seconds for the long TUNEs)
            double tolerance = seconds >= 60 ? 1.0 : 0.15;
            Assert.True(Math.Abs(duration.TotalSeconds - seconds) <= tolerance,
                $"{name}: got {duration.TotalSeconds:F2}s, documented {seconds}s");
            Assert.True(loops == actualLoops, $"{name}: loop flag mismatch (got {actualLoops})");
        }
    }

    /// <summary>Verifies seamless looping buffer geometry containing intro and one full loop body.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Looper_buffer_holds_intro_plus_one_loop_body()
    {
        var dats = ReadAllDats();

        var looper = LdsSynthesizer.TryCreate(dats["J_LOOP.DAT"]);
        Assert.NotNull(looper);
        Assert.True(looper!.Loops);
        Assert.InRange(looper.Duration.TotalSeconds, 17.9, 18.5);
        Assert.InRange((double)looper.LoopStartSample / looper.SampleRate, 8.95, 9.25);
        looper.EnsureRendered(looper.TotalSamples);
        Assert.InRange((double)looper.LoopStartSample / looper.SampleRate, 8.95, 9.25); // exact value, same window

        var oneShot = LdsSynthesizer.TryCreate(dats["J_DIED.DAT"]);
        Assert.NotNull(oneShot);
        Assert.False(oneShot!.Loops);
        Assert.Equal(0, oneShot.LoopStartSample);
    }

    /// <summary>Guards that all looping tracks maintain non-empty loop bodies and crossfade tails.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void Every_looper_has_nonempty_loop_body_and_fade_tail()
    {
        var dats = ReadAllDats();

        int loopers = 0;
        foreach (var (name, data) in dats)
        {
            if (name.Equals("GPALFIX.DAT", StringComparison.OrdinalIgnoreCase)) continue;
            var synth = LdsSynthesizer.TryCreate(data);
            Assert.NotNull(synth);
            if (!synth!.Loops) continue;
            loopers++;

            Assert.True(synth.LoopEndSample > synth.LoopStartSample,
                $"{name}: degenerate loop body (end {synth.LoopEndSample} <= start {synth.LoopStartSample})");
            Assert.True(synth.TotalSamples >= synth.LoopEndSample + LdsSynthesizer.LoopCrossfadeSamples,
                $"{name}: missing crossfade tail material");
        }

        Assert.Equal(22, loopers);
    }

    /// <summary>End-to-end synthesis smoke test: a short jingle renders non-silent PCM of the right length.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void Render_produces_audio()
    {
        var dats = ReadAllDats();

        var track = LdsRenderer.Render(dats["J_DIED.DAT"]);
        Assert.NotNull(track);
        Assert.False(track!.Loops);
        Assert.InRange(track.Duration.TotalSeconds, 4.0, 4.6);
        Assert.InRange(track.Pcm.Length, track.SampleRate * 4.0, track.SampleRate * 4.6);
        Assert.Contains(track.Pcm, s => Math.Abs((int)s) > 500); // actually made sound
    }
}
