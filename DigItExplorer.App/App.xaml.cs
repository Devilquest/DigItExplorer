using System.Configuration;
using System.Data;
using System.Windows;
using DigItExplorer.App.Models;
using DigItExplorer.App.Services;
using DigItExplorer.App.Views;
using DigItExplorer.Core.Ui;

namespace DigItExplorer.App;

internal partial class App : Application
{
    /// <summary>Constructs settings, initializes main window dependency injection, and displays the application.</summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var preferences = PreferencesStore.Load();
        var settings = new SessionSettings(
            PreferenceRules.ClampVolume(preferences.AudioVolume),
            PreferenceRules.ClampLastAudibleVolume(preferences.LastAudibleVolume),
            PreferenceRules.ClampExportScale(preferences.ExportScale),
            PreferenceRules.ParseEnumOrDefault(preferences.ExportFormatName, ExportFormat.CurrentFrame),
            PreferenceRules.ParseEnumOrDefault(preferences.ChosenPanelTabName, PanelTab.Layers));
        MainWindow = new MainWindow(settings, preferences);
        MainWindow.Show();
    }
}

