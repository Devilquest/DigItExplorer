using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.Core.Minigames;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.ViewModels.Layers;

/// <summary>Manages the layer tree and visibility settings for bonus minigame boards.</summary>
internal sealed class MinigameLayerForest(SessionSettings settings, MinigameBoard board) : LayerForest(settings)
{
    // One scope for the four boards, so the background and the attempt counters they share are one choice.
    private const string ScopeName = "Minigame";

    protected override string Scope => ScopeName;

    public override bool SameShapeAs(LayerForest other)
        => other is MinigameLayerForest minigame && minigame.Board == board;

    private MinigameBoard Board => board;

    public override List<LayerNode> Build()
    {
        List<LayerNode> baseChildren = board switch
        {
            MinigameBoard.SpinIt =>
            [
                LayerNode.Leaf("Background", MapLayer.MinigameBackground),
                LayerNode.Leaf("Pointer", MapLayer.SpinItPointer),
                LayerNode.Leaf("Attempts", MapLayer.MinigameAttempts),
            ],
            MinigameBoard.FlipIt =>
            [
                LayerNode.Leaf("Background", MapLayer.MinigameBackground),
                LayerNode.Leaf("Ropes", MapLayer.FlipItRopes),
                LayerNode.Leaf("Cards", MapLayer.FlipItCards),
                LayerNode.Leaf("Attempts", MapLayer.MinigameAttempts),
            ],
            MinigameBoard.StopIt =>
            [
                LayerNode.Leaf("Background", MapLayer.MinigameBackground),
                LayerNode.Leaf("Pieces", MapLayer.StopItPieces),
                LayerNode.Leaf("Labels", MapLayer.StopItLabels),
                LayerNode.Leaf("Attempts", MapLayer.MinigameAttempts),
            ],
            MinigameBoard.FindIt =>
            [
                LayerNode.Leaf("Background", MapLayer.MinigameBackground),
                LayerNode.Leaf("Pieces", MapLayer.FindItPieces),
                LayerNode.Leaf("Labels", MapLayer.FindItLabels),
                LayerNode.Leaf("Attempts", MapLayer.MinigameAttempts),
            ],
            _ => [],
        };
        return [BaseGroup(baseChildren)];
    }

    /// <summary>Extracts Spin It! render options from session settings.</summary>
    internal static SpinItRenderOptions SpinItOptionsFrom(SessionSettings s)
        => new(Visible(s, MapLayer.MinigameBackground), Visible(s, MapLayer.SpinItPointer),
            Visible(s, MapLayer.MinigameAttempts));

    /// <summary>Extracts Flip It! render options from session settings.</summary>
    internal static FlipItRenderOptions FlipItOptionsFrom(SessionSettings s)
        => new(Visible(s, MapLayer.MinigameBackground), Visible(s, MapLayer.FlipItRopes),
            Visible(s, MapLayer.FlipItCards), Visible(s, MapLayer.MinigameAttempts));

    /// <summary>Extracts Stop It! render options from session settings.</summary>
    internal static StopItRenderOptions StopItOptionsFrom(SessionSettings s)
        => new(Visible(s, MapLayer.MinigameBackground), Visible(s, MapLayer.StopItPieces),
            Visible(s, MapLayer.StopItLabels), Visible(s, MapLayer.MinigameAttempts));

    /// <summary>Extracts Find It! render options from session settings.</summary>
    internal static FindItRenderOptions FindItOptionsFrom(SessionSettings s)
        => new(Visible(s, MapLayer.MinigameBackground), Visible(s, MapLayer.FindItPieces),
            Visible(s, MapLayer.FindItLabels), Visible(s, MapLayer.MinigameAttempts));

    private static bool Visible(SessionSettings s, MapLayer layer) => s.IsShown(layer);
}
