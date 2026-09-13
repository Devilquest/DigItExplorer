using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Maps;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="ExitSignCompositor"/> and the palette independence that lets it be one node.</summary>
public class ExitSignCompositorTests
{
    private static ResourceLibrary OpenLibrary()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        return ResourceLibrary.Open(gameDir);
    }

    /// <summary>Verifies that exit sign composition preserves transparent padding around the sign graphic.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void The_sign_composes_with_its_padding_left_uncovered()
    {
        using var library = OpenLibrary();

        var result = ExitSignCompositor.Compose(library.TryRead);

        Assert.NotNull(result);
        Assert.True(result!.ShowedGraphic);
        Assert.False(result.ShowedCollision);
        Assert.Contains<byte>(255, result.Alpha);
        Assert.Contains<byte>(0, result.Alpha);
    }

    /// <summary>Guards that water level palette variations do not noticeably alter exit sign colors.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.FullResourceSet)]
    public void No_water_level_moves_the_signs_colors_perceptibly()
    {
        using var library = OpenLibrary();

        var frame = SheetImage.Read(library.Read(ExitSignCompositor.Sheet)).Frames[0];
        var used = new SortedSet<byte>(SpriteSheetSlicer.RectCell(frame, 0, 123, 27, 149).Pixels.Where(p => p != 0));
        Assert.NotEmpty(used);

        var reference = VgaPalette.From6Bit(
            library.Read(ExitSignCompositor.Palette).AsSpan(0, 768));
        // the world the sign appears in, and the only one
        var waterPals = library.Names.Where(n =>
            n.StartsWith("LVL2", StringComparison.OrdinalIgnoreCase) &&
            n.EndsWith(".PAL", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.NotEmpty(waterPals);

        int worst = 0;
        foreach (var name in waterPals)
        {
            var pal = VgaPalette.From6Bit(library.Read(name).AsSpan(0, 768));
            foreach (byte i in used)
            {
                var (ar, ag, ab) = reference[i];
                var (br, bg, bb) = pal[i];
                worst = Math.Max(worst, Math.Max(Math.Abs(ar - br), Math.Max(Math.Abs(ag - bg), Math.Abs(ab - bb))));
            }
        }

        Assert.True(worst <= 4, $"a water level moves the sign's colors by {worst} of 255");
    }
}
