using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SideSnap.Models;
using SideSnap.Services;

namespace SideSnap.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IStartupService _startupService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private bool _autoHide;

    [ObservableProperty]
    private bool _startWithWindows;

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (value)
        {
            _startupService.EnableStartup();
        }
        else
        {
            _startupService.DisableStartup();
        }
    }

    [ObservableProperty]
    private bool _darkMode;

    [ObservableProperty]
    private double _opacity;

    [ObservableProperty]
    private int _styleIndex;

    [ObservableProperty]
    private bool _showLabelByDefault;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private string _gradientColor1 = "99,102,241";

    [ObservableProperty]
    private string _gradientColor2 = "168,85,247";

    [ObservableProperty]
    private string _gradientColor3 = "236,72,153";

    [ObservableProperty]
    private int _iconPackIndex;

    public SettingsViewModel(
        ISettingsService settingsService,
        IStartupService startupService,
        ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _startupService = startupService;
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
        IsLocked = settings.IsLocked;
        GradientColor1 = settings.GradientColor1;
        GradientColor2 = settings.GradientColor2;
        GradientColor3 = settings.GradientColor3;

        // Map icon pack name to index
        var packNames = new[] { "Default", "Minimal", "Colorful", "Professional" };
        IconPackIndex = Array.IndexOf(packNames, settings.IconPack);
        if (IconPackIndex < 0) IconPackIndex = 0;

        _logger.LogDebug("Settings loaded");
    }

    public void SaveSettings()
    {
        // Load current settings to preserve width/height
        var currentSettings = _settingsService.LoadSettings();

        var settings = new AppSettings
        {
            AutoHide = AutoHide,
            StartWithWindows = StartWithWindows,
            DarkMode = DarkMode,
            Opacity = Opacity,
            Style = (AppStyle)StyleIndex,
            ShowLabelByDefault = ShowLabelByDefault,
            IsLocked = IsLocked,
            SidebarWidth = currentSettings.SidebarWidth,
            SidebarHeight = currentSettings.SidebarHeight,
            GradientColor1 = GradientColor1,
            GradientColor2 = GradientColor2,
            GradientColor3 = GradientColor3,
            IconPack = new[] { "Default", "Minimal", "Colorful", "Professional" }[IconPackIndex]
        };

        _settingsService.SaveSettings(settings);
        _logger.LogInformation("Settings saved");
    }
}