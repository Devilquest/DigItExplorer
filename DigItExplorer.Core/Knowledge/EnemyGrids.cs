namespace DigItExplorer.Core.Knowledge;

/// <summary>Uniform-grid geometry of an enemy sprite sheet including column count, strides, and cell dimensions.</summary>
public sealed record EnemyGrid(string Sheet, int Cols, int StrideX, int StrideY, int W, int H);

/// <summary>Maps DLF entity category bytes to their corresponding sprite sheet grid dimensions.</summary>
internal static class EnemyGrids
{
    private static readonly Dictionary<byte, EnemyGrid> ByCategory = new()
    {
        [0x06] = new("WO_SNL", 10, 32, 19, 32, 18), // Slugger
        [0x07] = new("WO_DRG", 8, 39, 37, 38, 36),  // Draggo (Drakko on underworld: same code, different world-suffixed file)
        [0x08] = new("WO_HRD", 10, 32, 28, 32, 27), // Rocker
        [0x09] = new("WO_RED", 6, 46, 41, 45, 40),  // Pyrosaur
        [0x0A] = new("WO_NRP", 8, 37, 30, 36, 29),  // Nirp
        [0x0B] = new("WO_NRP", 8, 37, 30, 36, 29),  // Nirpling
        [0x0C] = new("WO_SPK", 9, 35, 22, 34, 21),  // Spurk
        [0x0D] = new("WO_HOP", 10, 32, 31, 31, 30), // Hopper
        [0x0E] = new("WO_POP", 7, 44, 43, 43, 42),  // Papa Spurk
        [0x0F] = new("WO_THR", 6, 52, 36, 51, 35),  // Troggi
        [0x10] = new("WO_BAT", 6, 48, 23, 47, 22),  // Grock
    };

    /// <summary>Retrieves the sprite sheet grid for a specified DLF category.</summary>
    public static bool TryGet(byte category, out EnemyGrid grid)
        => ByCategory.TryGetValue(category, out grid!);
}
