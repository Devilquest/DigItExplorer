using DigItExplorer.Core.Catalog;
using static DigItExplorer.Core.Catalog.ExeLayout;

namespace DigItExplorer.Core.Knowledge;

/// <summary>Horizontal text alignment matching font engine draw routines.</summary>
public enum TextAlign { Left, Center, Right }

/// <summary>Caption text, screen anchor coordinates, alignment, and ink ramp style for an ending screen.</summary>
public sealed record EndSequenceCaption(byte[] Text, int X, int Y, TextAlign Align, int Style);

/// <summary>Executable call site addresses and alignment metadata for an ending caption.</summary>
/// <param name="Site">Call site holding the string and coordinate parameters.</param>
/// <param name="StyleSite">Ramp-select call site for ink coloring.</param>
/// <param name="Align">Horizontal alignment routine targeted by the draw call.</param>
internal readonly record struct EndSequenceCaptionSite(ExeAddress Site, ExeAddress StyleSite, TextAlign Align);

/// <summary>Executable call site catalog for all 10 ending sequence credits screens.</summary>
internal static class EndSequenceCatalog
{
    /// <summary>Caption call sites for each of the 10 ending sequence screens in display order.</summary>
    public static readonly IReadOnlyList<IReadOnlyList<EndSequenceCaptionSite>> Sites =
    [
        [ // Screen 1 -- Dug & Dugette reunited
            new(Seg2(0x1C5E), Seg2(0x1C4F), TextAlign.Center),
        ],
        [ // Screen 2 -- Slugger / Draggo / Rocker
            new(Seg2(0x1C76), Seg2(0x1C4F), TextAlign.Left),
            new(Seg2(0x1C84), Seg2(0x1C4F), TextAlign.Right),
            new(Seg2(0x1C93), Seg2(0x1C4F), TextAlign.Left),
            new(Seg2(0x1CA9), Seg2(0x1CA2), TextAlign.Left),
            new(Seg2(0x1CB7), Seg2(0x1CA2), TextAlign.Right),
            new(Seg2(0x1CC6), Seg2(0x1CA2), TextAlign.Left),
        ],
        [ // Screen 3 -- Spurk / Hopper / Nirp
            new(Seg2(0x1CDD), Seg2(0x1C4F), TextAlign.Right),
            new(Seg2(0x1CEC), Seg2(0x1C4F), TextAlign.Left),
            new(Seg2(0x1CFA), Seg2(0x1C4F), TextAlign.Right),
            new(Seg2(0x1D11), Seg2(0x1D0A), TextAlign.Right),
            new(Seg2(0x1D20), Seg2(0x1D0A), TextAlign.Left),
            new(Seg2(0x1D2E), Seg2(0x1D0A), TextAlign.Right),
        ],
        [ // Screen 4 -- the four Ghosts
            new(Seg2(0x1D49), Seg2(0x1C4F), TextAlign.Center),
            new(Seg2(0x1D57), Seg2(0x1C4F), TextAlign.Center),
            new(Seg2(0x1D66), Seg2(0x1C4F), TextAlign.Center),
            new(Seg2(0x1D75), Seg2(0x1C4F), TextAlign.Center),
            new(Seg2(0x1D8C), Seg2(0x1D85), TextAlign.Center),
            new(Seg2(0x1D9A), Seg2(0x1D85), TextAlign.Center),
            new(Seg2(0x1DA9), Seg2(0x1D85), TextAlign.Center),
            new(Seg2(0x1DB8), Seg2(0x1D85), TextAlign.Center),
        ],
        [ // Screen 5 -- Aqua Slugger / Sea Draggo / Rockerfish
            new(Seg2(0x1DD0), Seg2(0x1C4F), TextAlign.Left),
            new(Seg2(0x1DDE), Seg2(0x1C4F), TextAlign.Right),
            new(Seg2(0x1DED), Seg2(0x1C4F), TextAlign.Left),
            new(Seg2(0x1E03), Seg2(0x1DFC), TextAlign.Left),
            new(Seg2(0x1E11), Seg2(0x1DFC), TextAlign.Right),
            new(Seg2(0x1E20), Seg2(0x1DFC), TextAlign.Left),
        ],
        [ // Screen 6 -- Sea Spurk / Hopperfish / Nirpies
            new(Seg2(0x1E37), Seg2(0x1C4F), TextAlign.Right),
            new(Seg2(0x1E46), Seg2(0x1C4F), TextAlign.Left),
            new(Seg2(0x1E54), Seg2(0x1C4F), TextAlign.Right),
            new(Seg2(0x1E6B), Seg2(0x1E64), TextAlign.Right),
            new(Seg2(0x1E7A), Seg2(0x1E64), TextAlign.Left),
            new(Seg2(0x1E88), Seg2(0x1E64), TextAlign.Right),
        ],
        [ // Screen 7 -- Troggi / Papa Spurk
            new(Seg2(0x1EA0), Seg2(0x1C4F), TextAlign.Left),
            new(Seg2(0x1EAE), Seg2(0x1C4F), TextAlign.Right),
            new(Seg2(0x1EC5), Seg2(0x1EBE), TextAlign.Left),
            new(Seg2(0x1ED3), Seg2(0x1EBE), TextAlign.Right),
        ],
        [ // Screen 8 -- Drakko / Grock / Pyrosaur
            new(Seg2(0x1EEB), Seg2(0x1C4F), TextAlign.Right),
            new(Seg2(0x1EFA), Seg2(0x1C4F), TextAlign.Left),
            new(Seg2(0x1F08), Seg2(0x1C4F), TextAlign.Right),
            new(Seg2(0x1F1F), Seg2(0x1F18), TextAlign.Right),
            new(Seg2(0x1F2E), Seg2(0x1F18), TextAlign.Left),
            new(Seg2(0x1F3C), Seg2(0x1F18), TextAlign.Right),
        ],
        [ // Screen 9 -- Supreme Spurkasaur
            new(Seg2(0x1F54), Seg2(0x1C4F), TextAlign.Center),
            new(Seg2(0x1F6B), Seg2(0x1F64), TextAlign.Center),
        ],
        [ // Screen 10 -- the cheat-codes reveal
            new(Seg2(0x1F86), Seg2(0x1C4F), TextAlign.Center),
            new(Seg2(0x1F9C), Seg2(0x1F95), TextAlign.Center),
            new(Seg2(0x1FAB), Seg2(0x1F95), TextAlign.Center),
            new(Seg2(0x1FBA), Seg2(0x1F95), TextAlign.Right),
            new(Seg2(0x1FC8), Seg2(0x1F95), TextAlign.Left),
            new(Seg2(0x1FD7), Seg2(0x1F95), TextAlign.Right),
            new(Seg2(0x1FE5), Seg2(0x1F95), TextAlign.Left),
            new(Seg2(0x1FF4), Seg2(0x1F95), TextAlign.Right),
            new(Seg2(0x2003), Seg2(0x1F95), TextAlign.Left),
        ],
    ];
}
