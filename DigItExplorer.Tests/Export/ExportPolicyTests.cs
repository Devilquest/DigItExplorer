using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Export;

namespace DigItExplorer.Tests.Export;

/// <summary>Verifies export eligibility and audio file exclusion policies in <see cref="ExportPolicy"/>.</summary>
public class ExportPolicyTests
{
    [Theory]
    [InlineData("MUSIC01.DAT")]
    [InlineData("DIG.SMP")]
    [InlineData("gpalfix.dat")]
    [InlineData("Dig.Smp")]
    public void Audio_resources_may_never_be_exported(string name)
    {
        Assert.True(ExportPolicy.IsAudio(name));
        Assert.False(ExportPolicy.MayExport(name));
    }

    [Theory]
    [InlineData("LVL000F.MPF")]
    [InlineData("WO_DRG00.SPF")]
    [InlineData("DIGTITLE.ANI")]
    [InlineData("STORY.TXT")]
    public void Everything_else_may(string name)
    {
        Assert.False(ExportPolicy.IsAudio(name));
        Assert.True(ExportPolicy.MayExport(name));
    }

    /// <summary>Guards that audio resource extensions are never classified as renderable.</summary>
    [Theory]
    [InlineData("MUSIC01.DAT")]
    [InlineData("DIG.SMP")]
    public void No_audio_extension_is_renderable(string name)
        => Assert.False(ResourceLibrary.IsRenderable(name));
}
