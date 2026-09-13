using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DigItExplorer.App.ViewModels;

namespace DigItExplorer.App.Views;

/// <summary>Transport control bar for cutscene playback and frame scrubbing.</summary>
internal partial class CutsceneTransport : UserControl
{
    private bool _sliderUpdatingFromViewModel;

    public CutsceneTransport()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is CutscenePlayerViewModel oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            oldVm.IsSeekBarCaptured = null;
        }
        if (e.NewValue is CutscenePlayerViewModel newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
            newVm.IsSeekBarCaptured = () => CutsceneFrameSlider.IsMouseCaptureWithin;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CutscenePlayerViewModel.CurrentIteration)) return;
        if (sender is not CutscenePlayerViewModel vm) return;
        _sliderUpdatingFromViewModel = true;
        CutsceneFrameSlider.Value = vm.CurrentIteration;
        _sliderUpdatingFromViewModel = false;
    }

    private void CutsceneFrameSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_sliderUpdatingFromViewModel) return;
        if (DataContext is CutscenePlayerViewModel vm) vm.Seek((int)e.NewValue);
    }
}
