using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DigItExplorer.App.ViewModels;

namespace DigItExplorer.App.Views;

/// <summary>Transport control bar for audio sound effect and music playback.</summary>
internal partial class AudioTransport : UserControl
{
    private bool _seekBarUpdatingFromViewModel;

    public AudioTransport()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is AudioPlayerViewModel oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            oldVm.IsSeekBarCaptured = null;
        }
        if (e.NewValue is AudioPlayerViewModel newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
            newVm.IsSeekBarCaptured = () => AudioSeekSlider.IsMouseCaptureWithin;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AudioPlayerViewModel.SeekPosition)) return;
        if (sender is not AudioPlayerViewModel vm) return;
        _seekBarUpdatingFromViewModel = true;
        AudioSeekSlider.Value = vm.SeekPosition;
        _seekBarUpdatingFromViewModel = false;
    }

    private void AudioSeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_seekBarUpdatingFromViewModel) return;
        if (DataContext is AudioPlayerViewModel vm) vm.Seek((long)e.NewValue);
    }
}
