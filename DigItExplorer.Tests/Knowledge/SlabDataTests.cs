using System.Text;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Tests;

/// <summary>Guards slab screen metrics and string template parsing from DS:0x10 and executable tables.</summary>
public class SlabDataTests
{
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Both_screens_read_a_stop_count_that_fits_the_canvas()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        var slab = data.Slab;
        Assert.Equal(FrameCodec.Width, slab.Width);
        Assert.True(slab.Height > 0 && slab.Height % FrameCodec.Height == 0);

        foreach (int screen in new[] { SlabData.Instructions, SlabData.Credits })
        {
            int count = slab.SlabCount(screen);
            Assert.True(count >= 1);
            Assert.True((count - 1) * SlabData.SlabStep + FrameCodec.Height <= slab.Height);
        }
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void The_counter_template_splits_into_a_non_empty_prefix_and_infix()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        var slab = data.Slab;
        Assert.NotEmpty(slab.LabelPrefix);
        Assert.NotEmpty(slab.LabelInfix);
        Assert.All(slab.LabelPrefix, b => Assert.InRange(b, (byte)' ', (byte)'~'));
        Assert.All(slab.LabelInfix, b => Assert.InRange(b, (byte)' ', (byte)'~'));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void The_parallax_file_name_is_well_formed()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        Assert.Matches("^PARA[0-9]{2}B\\.MPF$", data.Slab.ParallaxFile);
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Reads_the_counts_and_text_this_build_ships() // build fingerprint, not a guard: see the class summary
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        var slab = data.Slab;
        Assert.Equal(6, slab.SlabCount(SlabData.Instructions));
        Assert.Equal(2, slab.SlabCount(SlabData.Credits));
        Assert.Equal(320, slab.Width);
        Assert.Equal(1600, slab.Height);
        Assert.Equal("PARA01B.MPF", slab.ParallaxFile);
        Assert.Equal("Slab ", Encoding.Latin1.GetString(slab.LabelPrefix));
        Assert.Equal(" of ", Encoding.Latin1.GetString(slab.LabelInfix));
    }
}
