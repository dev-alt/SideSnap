using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SideSnap.Models;
using SideSnap.Services;

namespace SideSnap.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private bool _autoHide;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _darkMode;

    [ObservableProperty]
    private double _opacity;

    [ObservableProperty]
    private int _styleIndex;

    [ObservableProperty]
    private bool _showLabelByDefault;

    public SettingsViewModel(ISettingsService settingsService, ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _logger = logger;

        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.LoadSettings();
        AutoHide = settings.AutoHide;
        StartWithWindows = settings.StartWithWindows;
        DarkMode = settings.DarkMode;
        Opacity = settings.Opacity;
        StyleIndex = (int)settings.Style;
        ShowLabelByDefault = settings.ShowLabelByDefault;

        _logger.LogDebug("Settings loaded");
    }

    public void SaveSettings()
    {
        var settings = new AppSettings
        {
            AutoHide = AutoHide,
            StartWithWindows = StartWithWindows,
            DarkMode = DarkMode,
            Opacity = Opacity,
            Style = (AppStyle)StyleIndex,
            ShowLabelByDefault = ShowLabelByDefault,
            SidebarWidth = 105
        };

        _settingsService.SaveSettings(settings);
        _logger.LogInformation("Settings saved");
    }
}