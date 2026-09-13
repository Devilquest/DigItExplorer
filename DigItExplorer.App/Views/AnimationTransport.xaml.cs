using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DigItExplorer.App.Models;
using DigItExplorer.App.ViewModels;

namespace DigItExplorer.App.Views;

/// <summary>Transport bar and timeline control for sprite animation playback.</summary>
internal partial class AnimationTransport : UserControl
{
    public AnimationTransport()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is AnimationPlayerViewModel oldVm) oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        if (e.NewValue is AnimationPlayerViewModel newVm) newVm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AnimationPlayerViewModel.CurrentStepIndex)) return;
        if (sender is not AnimationPlayerViewModel vm) return;
        if (AnimTimeline.ItemContainerGenerator.ContainerFromIndex(vm.CurrentStepIndex) is FrameworkElement el)
            el.BringIntoView();
    }

    private void TimelineStep_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not AnimationPlayerViewModel vm) return;
        if (sender is FrameworkElement { DataContext: TimelineStep step })
        {
            vm.JumpToStep(step.Index);
            e.Handled = true;
        }
    }
}
