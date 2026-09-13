using DigItExplorer.App.ViewModels.Layers;
using DigItExplorer.Core.Minigames;

namespace DigItExplorer.App.ViewModels.Documents;

/// <summary>Composed still document representing a bonus minigame board.</summary>
internal sealed class MinigameBoardStill(PreviewContext context, MinigameBoard board, IReadOnlyList<string> infoPath)
    : ComposedStill(context)
{
    public override ZoomBucket Bucket => ZoomBucket.ScreensUi;

    protected override IReadOnlyList<string> InfoPath => infoPath;

    protected override ComposedFrame? Compose()
    {
        MinigameBoardFrame? result = board switch
        {
            MinigameBoard.SpinIt => SpinItBoardCompositor.Compose(Context.Library.TryRead,
                MinigameLayerForest.SpinItOptionsFrom(Context.Settings)),
            MinigameBoard.FlipIt => FlipItBoardCompositor.Compose(Context.Library.TryRead,
                MinigameLayerForest.FlipItOptionsFrom(Context.Settings)),
            MinigameBoard.StopIt when Context.Font is not null && Context.Data is not null =>
                StopItBoardCompositor.Compose(Context.Library.TryRead, Context.Font, Context.Data.StopItLabels,
                    MinigameLayerForest.StopItOptionsFrom(Context.Settings)),
            MinigameBoard.FindIt when Context.Font is not null && Context.Data is not null =>
                FindItBoardCompositor.Compose(Context.Library.TryRead, Context.Font, Context.Data.FindItStartLabel,
                    MinigameLayerForest.FindItOptionsFrom(Context.Settings)),
            _ => null,
        };
        return result is null ? null : new ComposedFrame(result.Rgb, result.Width, result.Height, result.Alpha);
    }
}
