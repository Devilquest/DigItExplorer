using System.Windows;
using System.Windows.Controls.Primitives;

namespace DigItExplorer.App.Views;

/// <summary>Helper providing custom popup placement for top menu dropdowns.</summary>
internal static class MenuPopupPlacement
{
    /// <summary>Positions popup dropdown flush with header bottom-left corner.</summary>
    public static CustomPopupPlacementCallback BelowLeftEdge { get; } = (popupSize, targetSize, offset) =>
        [new CustomPopupPlacement(new Point(0, targetSize.Height), PopupPrimaryAxis.Horizontal)];
}
