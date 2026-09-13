using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using DigItExplorer.App.Interop;
using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.App.ViewModels;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App.Views;

/// <summary>Main application shell window managing view composition and lifecycle.</summary>
internal partial class MainWindow : Window
{
    private const double SidePanelWidth = 240;

    private readonly MainViewModel _main;
    private readonly PanelViewModel _panel;

    // WPF keeps its own flag for whether access keys are underlined and does not expose it. This is that
    // flag, and it says how the menu on screen was opened rather than what happened earlier in the session:
    // Alt and F10 turn it on, the mouse turns it off, and Windows can have the underlines on throughout.
    private bool _keyboardCues = SystemParameters.KeyboardCues;

    internal MainWindow(SessionSettings settings, PreferencesStore preferences)
    {
        InitializeComponent();

        var checkerBrush = (Brush)FindResource("CheckerBrush");
        var layers = new LayersViewModel(settings);
        var info = new InfoViewModel();

        var audio = new AudioPlayerViewModel(settings, PreviewStageControl);
        AudioTransportControl.DataContext = audio;

        var anim = new AnimationPlayerViewModel(PreviewStageControl, settings, info, checkerBrush);
        AnimationTransportControl.DataContext = anim;

        var story = new StoryScenePlayerViewModel(PreviewStageControl, settings, info);
        StoryTransportControl.DataContext = story;

        var cutscene = new CutscenePlayerViewModel(PreviewStageControl, settings, info);
        CutsceneTransportControl.DataContext = cutscene;

        var panel = new PanelViewModel(settings, layers, info, preferences.IsPanelVisible);
        _panel = panel;
        LayersPaneControl.DataContext = layers;
        InfoPaneControl.DataContext = info;
        SidePanel.DataContext = panel;
        PreviewStageControl.DataContext = panel;
        panel.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(PanelViewModel.IsAvailable):
                    UpdateSidePanelVisibility(animate: false);
                    break;
                case nameof(PanelViewModel.IsPanelVisible):
                    UpdateSidePanelVisibility(animate: true);
                    break;
            }
        };

        var nav = new NavigationViewModel();
        NavigationPaneControl.DataContext = nav;

        _main = new MainViewModel(PreviewStageControl, settings, preferences, audio, anim, story, cutscene, panel, nav);
        DataContext = _main;

        InputBindings.Add(new KeyBinding(_main.OpenGameFolderCommand, Key.O, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(_main.Export.ExportCommand, Key.E, ModifierKeys.Control));
        InputManager.Current.PreProcessInput += OnPreProcessInput;

        CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (_, _) => SystemCommands.CloseWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, (_, _) => SystemCommands.MinimizeWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, (_, _) => SystemCommands.MaximizeWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, (_, _) => SystemCommands.RestoreWindow(this)));

        NavigationPaneControl.ResourceSelected += (_, node) => _main.HandleResourceSelected(node);
        InfoPaneControl.FileRequested += (_, resource) =>
        {
            if (_main.ShowRawResource(resource) is { } node)
                NavigationPaneControl.Reveal(node, TreeTabs.Raw);
        };
        InfoPaneControl.BackRequested += (_, _) =>
        {
            if (_main.GoBack() is { } back) NavigationPaneControl.Reveal(back.Node, back.Tab);
        };
        PreviewStageControl.OpenGameFolderRequested += (_, _) => _main.OpenGameFolderCommand.Execute(null);
        PreviewStageControl.ZoomOrFitChanged += (_, _) =>
        {
            if (anim.HasAnimation || _main.HasSpriteDocument)
            {
                settings.AnimationZoom = PreviewStageControl.Zoom;
                settings.AnimationIsFitMode = PreviewStageControl.IsFitMode;
            }
            else if (_main.HasScreenDocument)
            {
                settings.ScreensZoom = PreviewStageControl.Zoom;
                settings.ScreensIsFitMode = PreviewStageControl.IsFitMode;
            }
            else if (_main.HasMapDocument)
            {
                settings.MapIsOneToOne = !PreviewStageControl.IsFitMode
                    && Math.Abs(PreviewStageControl.Zoom - 1.0) < 0.0005;
            }
        };

        SourceInitialized += (_, _) =>
        {
            DwmInterop.ApplyDarkTitleBar(this);
            DwmInterop.SquareOffCorners(this);
            MaximizeBounds.Attach(this);
            CaptionButtonHitTest.Attach(this, MaximizeRestoreButton);
            RestoreGeometry(preferences);
        };
        Loaded += (_, _) => _main.Initialize();
        Closing += (_, _) =>
        {
            SaveGeometry(preferences);
            SavePersonPreferences(preferences, settings, panel);
        };
        Closed += (_, _) =>
        {
            InputManager.Current.PreProcessInput -= OnPreProcessInput;
            audio.Hide();
            _main.Dispose();
        };
    }

    /// <summary>Opens the About box dialog.</summary>
    private void About_Click(object sender, RoutedEventArgs e) => AboutWindow.Show(this);

    /// <summary>Opens the dialog listing the other tools built for Dig It!.</summary>
    private void Tools_Click(object sender, RoutedEventArgs e) => ToolsWindow.Show(this);

    /// <summary>Opens the dialog listing every keyboard shortcut.</summary>
    private void Shortcuts_Click(object sender, RoutedEventArgs e) => ShortcutsWindow.Show(this);

    /// <summary>Opens the user guide window.</summary>
    private void UserGuide_Click(object sender, RoutedEventArgs e) => HelpWindow.Show(this);

    /// <summary>Handles F1 help command shortcut execution.</summary>
    private void UserGuide_Executed(object sender, ExecutedRoutedEventArgs e) => HelpWindow.Show(this);

    /// <summary>Closes the application.</summary>
    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>Takes the letters away with the underlines when the menu is opened with the mouse.</summary>
    private void MainMenu_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        => _keyboardCues = SystemParameters.KeyboardCues;

    /// <summary>Refuses a letter to an open menu that is underlining none, which is what a menu opened with
    /// the mouse does.</summary>
    private void OnPreProcessInput(object sender, PreProcessInputEventArgs e)
    {
        // Taken off the input queue rather than from a routed handler: the menu claims the key first, and
        // the access keys are read after every handler has had it, so there is nowhere later to refuse from.
        if (_keyboardCues || !IsMenuOpen()) return;

        bool letter = e.StagingItem.Input switch
        {
            KeyEventArgs key => key.Key is >= Key.A and <= Key.Z,
            TextCompositionEventArgs text => text.Text.Length > 0 && char.IsLetter(text.Text[0]),
            _ => false,
        };

        if (letter) e.Cancel();
    }

    private bool IsMenuOpen() => MainMenu.Items.OfType<MenuItem>().Any(item => item.IsSubmenuOpen);

    /// <summary>Routes a keystroke to the control its shortcut stands for.</summary>
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.SystemKey is Key.LeftAlt or Key.RightAlt or Key.F10 || e.Key is Key.F10) _keyboardCues = true;

        // An open menu is a mode: a key would act on the picture the user has just stepped away from.
        if (MainMenu.IsKeyboardFocusWithin) return;

        var input = new ShortcutInput(e.Key.ToString(),
            (Keyboard.Modifiers & ModifierKeys.Control) != 0,
            Keyboard.FocusedElement is TextBoxBase { IsReadOnly: false });

        if (Shortcuts.Resolve(input) is not { } shortcut) return;
        e.Handled = true;

        switch (shortcut.Action)
        {
            case ShortcutAction.ZoomIn: PreviewStageControl.ZoomIn(); break;
            case ShortcutAction.ZoomOut: PreviewStageControl.ZoomOut(); break;
            case ShortcutAction.FitToWindow: PreviewStageControl.Fit(); break;
            case ShortcutAction.ActualSize: PreviewStageControl.ActualSize(); break;
            case ShortcutAction.TogglePanel: _panel.TogglePanel(); break;
            case ShortcutAction.ToggleLayerGroup when shortcut.LayerGroup is { } group:
                _main.Layers.ToggleGroup(group);
                break;
            case ShortcutAction.PlayPause:
                _main.ToggleActivePlayback();
                break;
            case ShortcutAction.ToggleMute:
                _main.ToggleAudioMute();
                break;
        }
    }

    /// <summary>Sizes and centers window to 80% of current work area.</summary>
    private void SizeToWorkArea()
    {
        var workArea = SystemParameters.WorkArea;

        Width = Math.Min(Math.Max(workArea.Width * 0.80, MinWidth), workArea.Width);
        Height = Math.Min(Math.Max(workArea.Height * 0.80, MinHeight), workArea.Height);

        Left = workArea.Left + (workArea.Width - Width) / 2;
        Top = workArea.Top + (workArea.Height - Height) / 2;
    }

    /// <summary>Restores saved window geometry and maximized state.</summary>
    private void RestoreGeometry(PreferencesStore preferences)
    {
        var saved = preferences.SavedWindowRect;
        var virtualScreen = new WindowRect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        var monitorWorkArea = saved is { } s ? MonitorLayout.TryGetWorkArea(this, s) : null;

        var resolved = WindowPlacementRule.Resolve(saved, MinWidth, MinHeight, monitorWorkArea, virtualScreen);

        if (resolved is not { } rect)
        {
            SizeToWorkArea();
            return;
        }

        Left = rect.Left;
        Top = rect.Top;
        Width = rect.Width;
        Height = rect.Height;

        if (preferences.WindowIsMaximized) WindowState = WindowState.Maximized;
    }

    /// <summary>Saves window bounds and maximized state.</summary>
    private void SaveGeometry(PreferencesStore preferences)
    {
        if (RestoreBounds == Rect.Empty) return;

        var rect = new WindowRect(RestoreBounds.Left, RestoreBounds.Top, RestoreBounds.Width, RestoreBounds.Height);
        preferences.SaveWindowGeometry(rect, WindowState == WindowState.Maximized);
    }

    /// <summary>Saves user session settings and panel preferences.</summary>
    private static void SavePersonPreferences(PreferencesStore preferences, SessionSettings settings, PanelViewModel panel)
        => preferences.SavePersonPreferences(settings.AudioVolume, settings.LastAudibleVolume, settings.ExportScale,
            settings.ExportFormat.ToString(), settings.PanelTabs.Chosen.ToString(), panel.IsPanelVisible);

    /// <summary>Animates or toggles side panel visibility.</summary>
    private void UpdateSidePanelVisibility(bool animate)
    {
        if (SidePanel == null) return;
        var panel = (PanelViewModel)SidePanel.DataContext;

        bool shouldBeVisible = panel.IsAvailable && panel.IsPanelVisible;

        if (shouldBeVisible)
        {
            if (SidePanel.Visibility != Visibility.Visible)
            {
                SidePanel.Visibility = Visibility.Visible;
            }

            if (animate)
            {
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = SidePanel.ActualWidth > 0 ? SidePanel.ActualWidth : 0,
                    To = SidePanelWidth,
                    Duration = TimeSpan.FromMilliseconds(150),
                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
                };
                SidePanel.BeginAnimation(WidthProperty, animation);
            }
            else
            {
                SidePanel.BeginAnimation(WidthProperty, null);
                SidePanel.Width = SidePanelWidth;
                UpdateLayout();
            }
        }
        else
        {
            if (animate)
            {
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = SidePanel.ActualWidth,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(150),
                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
                };
                animation.Completed += (s, e) =>
                {
                    if (!panel.IsAvailable || !panel.IsPanelVisible)
                    {
                        SidePanel.Visibility = Visibility.Collapsed;
                    }
                };
                SidePanel.BeginAnimation(WidthProperty, animation);
            }
            else
            {
                SidePanel.BeginAnimation(WidthProperty, null);
                SidePanel.Width = 0;
                SidePanel.Visibility = Visibility.Collapsed;
                UpdateLayout();
            }
        }
    }
}
