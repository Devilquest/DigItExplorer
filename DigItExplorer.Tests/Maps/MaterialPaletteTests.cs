using DigItExplorer.Core.Maps;

namespace DigItExplorer.Tests;

/// <summary>Verifies <see cref="MaterialPalette"/>'s false-color assignments.</summary>
public class MaterialPaletteTests
{
    /// <summary>Verifies that material 31 draws as material 16 instead of falling back to a generated color.</summary>
    [Fact]
    public void Material_31_draws_as_material_16()
    {
        Assert.Equal(MaterialPalette.ColorOf(16), MaterialPalette.ColorOf(31));
        Assert.Equal(MaterialPalette.OverlayColorOf(16), MaterialPalette.OverlayColorOf(31));
    }
}
