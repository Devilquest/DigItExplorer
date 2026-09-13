namespace DigItExplorer.Core.Maps;

/// <summary>False-color visualization palette for raw collision material codes.</summary>
internal static class MaterialPalette
{
    private static readonly Dictionary<byte, (byte R, byte G, byte B, byte A)> Known = new()
    {
        [0] = (90, 60, 40, 255),      // solid rock/dirt mass
        [15] = (0, 0, 0, 255),        // open void / no collision
        [16] = (80, 200, 255, 255),   // platform-top edge marker
        [31] = (80, 200, 255, 255),   // the same marker as 16, as the water world writes it
        [32] = (255, 210, 0, 255),    // boundary wall
        [48] = (255, 0, 255, 255),    // outermost wall lining
        [64] = (255, 40, 40, 255),    // platform walkable surface
        [80] = (255, 140, 0, 255),    // rare, and with no established role: colored so it is at least visible
        [112] = (120, 120, 120, 255), // floor zone
    };

    /// <summary>Gets the opaque display color for a collision material code.</summary>
    public static (byte R, byte G, byte B, byte A) ColorOf(byte code)
        => Known.TryGetValue(code, out var c) ? c : Fallback(code);

    // Solid rock (0) and open void (15) are fully transparent to avoid washing out underlying terrain.
    private static readonly Dictionary<byte, byte> OverlayDimAlpha = new() { [0] = 0, [15] = 0 };
    private const byte OverlayDefaultAlpha = 200;

    /// <summary>Gets the display color with adjusted alpha for terrain overlay blending.</summary>
    public static (byte R, byte G, byte B, byte A) OverlayColorOf(byte code)
    {
        var (r, g, b, _) = ColorOf(code);
        byte a = OverlayDimAlpha.TryGetValue(code, out var dim) ? dim : OverlayDefaultAlpha;
        return (r, g, b, a);
    }

    private static (byte R, byte G, byte B, byte A) Fallback(byte code)
    {
        uint h = code * 2654435761u; // Knuth multiplicative hash; the uint overflow (wrap mod 2^32) is the point
        return (
            (byte)(80 + (h & 0x7F)),
            (byte)(80 + ((h >> 8) & 0x7F)),
            (byte)(80 + ((h >> 16) & 0x7F)),
            255);
    }
}
