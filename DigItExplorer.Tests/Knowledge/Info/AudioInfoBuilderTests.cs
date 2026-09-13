using DigItExplorer.Core.Audio;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Knowledge.Info;

namespace DigItExplorer.Tests;

/// <summary>Verifies audio metadata and contents section generation in <see cref="AudioInfoBuilder"/>.</summary>
public class AudioInfoBuilderTests
{
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void BuildMusic_produces_source_and_synthesis_contents_sections()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        const string tune = "TUNE7.DAT";
        var data = library.Read(tune);
        var synth = LdsSynthesizer.TryCreate(data);
        Assert.NotNull(synth);

        var info = AudioInfoBuilder.BuildMusic(tune, library, synth);

        var source = info.Sections.Single(s => s.Title == "Source");
        var contents = info.Sections.Single(s => s.Title == "Contents");

        Assert.Equal(tune, source.Rows[0].Label);
        Assert.Equal(tune, source.Rows[0].LabelFile);

        var format = contents.Rows.Single(r => r.Label == "Format");
        var rate = contents.Rows.Single(r => r.Label == "Sample rate");
        var duration = contents.Rows.Single(r => r.Label == "Duration");
        var mode = contents.Rows.Single(r => r.Label == "Mode");

        Assert.Equal("OPL2 FM synthesis", format.Value);
        Assert.Equal("44,100 Hz", rate.Value);
        Assert.Contains("min", duration.Value);
        Assert.Equal("Loop", mode.Value);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void BuildSound_produces_source_and_pcm_contents_sections()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        const string sfx = "FX_HRLDR.SMP";
        var length = library.Read(sfx).Length;

        var info = AudioInfoBuilder.BuildSound(sfx, library, length);

        var source = info.Sections.Single(s => s.Title == "Source");
        var contents = info.Sections.Single(s => s.Title == "Contents");

        Assert.Equal(sfx, source.Rows[0].Label);
        Assert.Equal(sfx, source.Rows[0].LabelFile);

        var format = contents.Rows.Single(r => r.Label == "Format");
        var rate = contents.Rows.Single(r => r.Label == "Sample rate");
        var duration = contents.Rows.Single(r => r.Label == "Duration");
        var mode = contents.Rows.Single(r => r.Label == "Mode");

        Assert.Equal("8-bit signed mono PCM", format.Value);
        Assert.Equal("11,025 Hz", rate.Value);
        Assert.Contains("s", duration.Value);
        Assert.Equal("Once", mode.Value);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void BuildMusic_without_synth_omits_contents_section()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        const string tune = "TUNE7.DAT";
        var info = AudioInfoBuilder.BuildMusic(tune, library, null);

        Assert.Single(info.Sections);
        Assert.Equal("Source", info.Sections[0].Title);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Audio_info_source_leads_to_itself_clears_row_link()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        const string sfx = "FX_HRLDR.SMP";
        var length = library.Read(sfx).Length;

        var info = AudioInfoBuilder.BuildSound(sfx, library, length, sourceLeadsToItself: true);
        var source = info.Sections.Single(s => s.Title == "Source");

        Assert.Null(source.Rows[0].LabelFile);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Looping_music_duration_reflects_single_pass_instead_of_cache_buffer()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);

        var tune0 = LdsSynthesizer.TryCreate(library.Read("TUNE0.DAT"));
        Assert.NotNull(tune0);
        var info0 = AudioInfoBuilder.BuildMusic("TUNE0.DAT", library, tune0);
        var duration0 = info0.Sections.Single(s => s.Title == "Contents").Rows.Single(r => r.Label == "Duration").Value;
        Assert.Equal("3:33 min", duration0);

        var super = LdsSynthesizer.TryCreate(library.Read("J_SUPER.DAT"));
        Assert.NotNull(super);
        var infoSuper = AudioInfoBuilder.BuildMusic("J_SUPER.DAT", library, super);
        var durationSuper = infoSuper.Sections.Single(s => s.Title == "Contents").Rows.Single(r => r.Label == "Duration").Value;
        Assert.Equal("12.48 s", durationSuper);
    }
}

