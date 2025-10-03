using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SideSnap.Models;
using SideSnap.Services;
using SideSnap.ViewModels;

namespace SideSnap.Views;

public partial class MainWindow
{
    private readonly ILogger<MainWindow> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISettingsService _settingsService;
    private readonly DispatcherTimer _hideTimer;
    private const int WmWindowPosChanging = 0x0046;
    private const double CollapsedWidth = 5;
    private const double ExpandedWidth = 105;
    private bool _isAutoHideEnabled;
    private bool _isExpanded = true;

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowPos
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(int uAction, int uParam, ref Rect lpvParam, int fuWinIni);

    private const int SpiSetworkarea = 0x002F;
    private const int SpiGetworkarea = 0x0030;
    private const int SpifUpdateinifile = 0x01;
    private const int SpifSendchange = 0x02;

    public MainWindow(MainViewModel viewModel, ILogger<MainWindow> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _settingsService = serviceProvider.GetRequiredService<ISettingsService>();

        InitializeComponent();
        DataContext = viewModel;

        _logger.LogInformation("MainWindow initializing");

        // Position window at left edge
        Left = 0;
        Top = 0;
        Width = ExpandedWidth;
        Height = SystemParameters.PrimaryScreenHeight;

        _logger.LogDebug("Window positioned at (0,0) with width {Width}px and height {Height}px", Width, Height);

        // Load settings
        var settings = _settingsService.LoadSettings();
        _isAutoHideEnabled = settings.AutoHide;

        // Apply lock/unlock state
        if (settings.IsLocked)
        {
            ResizeMode = ResizeMode.NoResize;
        }
        else
        {
            ResizeMode = ResizeMode.CanResizeWithGrip;
            if (settings.SidebarWidth > 0)
                Width = settings.SidebarWidth;
            if (settings.SidebarHeight > 0)
                Height = settings.SidebarHeight;
        }

        // Apply visual style
        ApplyVisualStyle(settings.Style, settings.DarkMode);

        // Setup auto-hide timer
        _hideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1500)
        };
        _hideTimer.Tick += HideTimer_Tick;

        // Setup popup safety timer to close orphaned popups
        _popupSafetyTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _popupSafetyTimer.Tick += PopupSafetyTimer_Tick;
        _popupSafetyTimer.Start();

        // Hook into window messages to prevent moving
        Loaded += MainWindow_Loaded;
        MouseEnter += MainWindow_MouseEnter;
        MouseLeave += MainWindow_MouseLeave;
        Closing += MainWindow_Closing;

        if (_isAutoHideEnabled)
        {
            _logger.LogInformation("Auto-hide enabled");
        }

        // Reserve screen space for sidebar
        ReserveScreenSpace();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        hwndSource?.AddHook(WndProc);
        _logger.LogInformation("Window message hook added to prevent dragging");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmWindowPosChanging)
        {
            var windowPos = Marshal.PtrToStructure<WindowPos>(lParam);

            // Lock position to (0, 0) but allow width changes for auto-hide
            if (windowPos.x != 0 || windowPos.y != 0)
            {
                windowPos.x = 0;
                windowPos.y = 0;
                Marshal.StructureToPtr(windowPos, lParam, true);
                _logger.LogDebug("Prevented window position change");
            }

            // Only enforce width if not in auto-hide mode or if it's not the expected collapsed/expanded width
            if (!_isAutoHideEnabled && windowPos.cx != (int)ExpandedWidth)
            {
                windowPos.cx = (int)ExpandedWidth;
                Marshal.StructureToPtr(windowPos, lParam, true);
                _logger.LogDebug("Prevented window width change");
            }
        }

        return IntPtr.Zero;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Prevent dragging by not calling DragMove()
        _logger.LogDebug("Mouse left button clicked - dragging disabled");
        e.Handled = true;
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Exit button clicked, closing application");
        System.Windows.Application.Current.Shutdown();
    }

    private void Window_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Window_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            var files = (string[]?)e.Data.GetData(System.Windows.DataFormats.FileDrop);
            if (files is { Length: > 0 })
            {
                var viewModel = DataContext as MainViewModel;
                if (viewModel is not null)
                {
                    foreach (var file in files)
                    {
                        _logger.LogInformation("File/folder dropped: {Path}", file);

                        // Check if it's a directory or file type
                        if (Directory.Exists(file))
                        {
                            viewModel.AddDroppedFolder(file);
                        }
                        else if (File.Exists(file))
                        {
                            var extension = Path.GetExtension(file).ToLower();
                            switch (extension)
                            {
                                case ".exe":
                                    viewModel.AddDroppedExecutable(file);
                                    break;
                                case ".lnk":
                                    viewModel.AddDroppedShortcut(file);
                                    break;
                                case ".sh":
                                    viewModel.AddDroppedShellScript(file);
                                    break;
                                case ".ps1":
                                    viewModel.AddDroppedPowerShellScript(file);
                                    break;
                                default:
                                    _logger.LogWarning("Unsupported file type dropped: {Extension}", extension);
                                    break;
                            }
                        }
                    }
                }
            }
        }
        e.Handled = true;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Opening settings window");

        // Save current size before opening settings
        var currentSettings = _settingsService.LoadSettings();
        if (!currentSettings.IsLocked)
        {
            currentSettings.SidebarWidth = Width;
            currentSettings.SidebarHeight = Height;
            _settingsService.SaveSettings(currentSettings);
        }

        var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
        settingsWindow.Owner = this;
        if (settingsWindow.ShowDialog() == true)
        {
            // Reload settings
            var settings = _settingsService.LoadSettings();
            _isAutoHideEnabled = settings.AutoHide;
            ApplyVisualStyle(settings.Style, settings.DarkMode);

            // Apply lock/unlock state
            if (settings.IsLocked)
            {
                ResizeMode = ResizeMode.NoResize;
            }
            else
            {
                ResizeMode = ResizeMode.CanResizeWithGrip;
            }

            _logger.LogInformation("Settings updated - Auto-hide: {AutoHide}, Style: {Style}, Locked: {Locked}",
                _isAutoHideEnabled, settings.Style, settings.IsLocked);

            if (!_isAutoHideEnabled && !_isExpanded)
            {
                ExpandSidebar();
            }
        }
    }

    private void ApplyVisualStyle(AppStyle style, bool darkMode)
    {
        var border = this.FindName("MainBorder") as System.Windows.Controls.Border;
        var header = this.FindName("HeaderBorder") as System.Windows.Controls.Border;
        if (border == null) return;

        // Base colors based on theme
        var lightBg = System.Windows.Media.Color.FromRgb(240, 240, 240);
        var darkBg = System.Windows.Media.Color.FromRgb(30, 30, 35);
        var baseBg = darkMode ? darkBg : lightBg;

        // Update header color
        if (header != null)
        {
            header.Background = new SolidColorBrush(darkMode
                ? System.Windows.Media.Color.FromRgb(20, 20, 25)
                : System.Windows.Media.Color.FromRgb(44, 62, 80));
        }

        switch (style)
        {
            case AppStyle.Solid:
                border.Background = new SolidColorBrush(baseBg);
                border.Effect = new DropShadowEffect
                {
                    ShadowDepth = 3,
                    BlurRadius = 12,
                    Opacity = darkMode ? 0.6 : 0.35,
                    Color = System.Windows.Media.Color.FromRgb(0, 0, 0)
                };
                _logger.LogDebug("Applied Solid style (DarkMode: {DarkMode})", darkMode);
                break;

            case AppStyle.Glass:
                // Enhanced glass effect with layered transparency
                var glassGradient = new LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(0, 1)
                };
                if (darkMode)
                {
                    glassGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(240, 40, 40, 50), 0));
                    glassGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(220, 30, 30, 40), 0.5));
                    glassGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(230, 25, 25, 35), 1));
                }
                else
                {
                    glassGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(240, 255, 255, 255), 0));
                    glassGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(220, 240, 245, 250), 0.5));
                    glassGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(230, 235, 240, 245), 1));
                }
                border.Background = glassGradient;

                var glassEffect = new DropShadowEffect
                {
                    ShadowDepth = 0,
                    BlurRadius = 25,
                    Opacity = 0.6,
                    Color = darkMode
                        ? System.Windows.Media.Color.FromArgb(180, 0, 0, 0)
                        : System.Windows.Media.Color.FromArgb(180, 255, 255, 255)
                };
                border.Effect = glassEffect;
                _logger.LogDebug("Applied Glass style (DarkMode: {DarkMode})", darkMode);
                break;

            case AppStyle.Acrylic:
                // Improved acrylic with subtle gradient and noise simulation
                var acrylicGradient = new LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(0, 1)
                };
                if (darkMode)
                {
                    acrylicGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(210, 35, 35, 45), 0));
                    acrylicGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(200, 30, 30, 40), 0.3));
                    acrylicGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(190, 25, 25, 35), 0.7));
                    acrylicGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(200, 20, 20, 30), 1));
                }
                else
                {
                    acrylicGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(210, 250, 250, 250), 0));
                    acrylicGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(200, 245, 245, 245), 0.3));
                    acrylicGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(190, 240, 240, 240), 0.7));
                    acrylicGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(200, 235, 235, 235), 1));
                }
                border.Background = acrylicGradient;

                var acrylicEffect = new DropShadowEffect
                {
                    ShadowDepth = 4,
                    BlurRadius = 30,
                    Opacity = darkMode ? 0.6 : 0.4,
                    Color = darkMode
                        ? System.Windows.Media.Color.FromRgb(0, 0, 0)
                        : System.Windows.Media.Color.FromRgb(120, 120, 120)
                };
                border.Effect = acrylicEffect;
                _logger.LogDebug("Applied Acrylic style (DarkMode: {DarkMode})", darkMode);
                break;

            case AppStyle.Gradient:
                // Beautiful gradient with custom or default colors
                var settings = _settingsService.LoadSettings();
                var gradientBrush = new LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(0, 1)
                };

                var color1 = ParseRgbColor(settings.GradientColor1, darkMode);
                var color2 = ParseRgbColor(settings.GradientColor2, darkMode);
                var color3 = ParseRgbColor(settings.GradientColor3, darkMode);

                gradientBrush.GradientStops.Add(new GradientStop(color1, 0));
                gradientBrush.GradientStops.Add(new GradientStop(color2, 0.5));
                gradientBrush.GradientStops.Add(new GradientStop(color3, 1));
                border.Background = gradientBrush;

                var gradientEffect = new DropShadowEffect
                {
                    ShadowDepth = 5,
                    BlurRadius = 20,
                    Opacity = darkMode ? 0.7 : 0.5,
                    Color = darkMode
                        ? System.Windows.Media.Color.FromRgb(0, 0, 0)
                        : System.Windows.Media.Color.FromRgb(99, 102, 241)
                };
                border.Effect = gradientEffect;
                _logger.LogDebug("Applied Gradient style (DarkMode: {DarkMode})", darkMode);
                break;

            case AppStyle.Neumorphism:
                // Neumorphism (Soft UI) with subtle shadows
                var neumoBackground = new SolidColorBrush(darkMode
                    ? System.Windows.Media.Color.FromRgb(35, 35, 42)
                    : System.Windows.Media.Color.FromRgb(230, 230, 235));
                border.Background = neumoBackground;

                // Create layered shadow effect for depth
                var neumoEffect = new DropShadowEffect
                {
                    ShadowDepth = 8,
                    BlurRadius = 16,
                    Opacity = darkMode ? 0.4 : 0.25,
                    Color = darkMode
                        ? System.Windows.Media.Color.FromRgb(10, 10, 15)
                        : System.Windows.Media.Color.FromRgb(180, 180, 190),
                    Direction = 135
                };
                border.Effect = neumoEffect;
                _logger.LogDebug("Applied Neumorphism style (DarkMode: {DarkMode})", darkMode);
                break;
        }
    }

    private void MainWindow_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isAutoHideEnabled && !_isExpanded)
        {
            _hideTimer.Stop();
            ExpandSidebar();
        }
    }

    private void MainWindow_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isAutoHideEnabled && _isExpanded)
        {
            _hideTimer.Start();
        }
    }

    private void HideTimer_Tick(object? sender, EventArgs e)
    {
        _hideTimer.Stop();
        if (_isAutoHideEnabled && _isExpanded)
        {
            CollapseSidebar();
        }
    }

    private void ExpandSidebar()
    {
        _isExpanded = true;
        AnimateWidth(ExpandedWidth);
        _logger.LogDebug("Sidebar expanded");
    }

    private void CollapseSidebar()
    {
        _isExpanded = false;
        AnimateWidth(CollapsedWidth);
        _logger.LogDebug("Sidebar collapsed");
    }

    private void AnimateWidth(double targetWidth)
    {
        var animation = new DoubleAnimation
        {
            To = targetWidth,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };

        BeginAnimation(WidthProperty, animation);
    }

    private System.Windows.Controls.Primitives.Popup? _currentOpenPopup;
    private DispatcherTimer? _popupCloseTimer;
    private DispatcherTimer? _popupSafetyTimer;

    private void ProjectButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // Cancel any pending close timer
        _popupCloseTimer?.Stop();

        if (sender is System.Windows.Controls.Button button && button.DataContext is Models.Project project)
        {
            // Close previous popup immediately (menu-style behavior)
            if (_currentOpenPopup != null && _currentOpenPopup.IsOpen)
            {
                _currentOpenPopup.IsOpen = false;
            }

            // Open this popup
            var grid = button.Parent as System.Windows.Controls.Grid;
            if (grid != null)
            {
                var popup = grid.FindName("ProjectDropdown") as System.Windows.Controls.Primitives.Popup;
                if (popup != null)
                {
                    _currentOpenPopup = popup;
                    popup.IsOpen = true;
                }
            }
        }
    }

    private void ProjectButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // Start delayed close (gives time to move mouse to popup)
        StartPopupCloseTimer();
    }

    private void ProjectPopup_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // Mouse entered popup, cancel close
        _popupCloseTimer?.Stop();
    }

    private void ProjectPopup_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // Mouse left popup, close immediately
        CloseCurrentPopup();
    }

    private void StartPopupCloseTimer()
    {
        _popupCloseTimer?.Stop();
        _popupCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _popupCloseTimer.Tick += (s, args) =>
        {
            _popupCloseTimer?.Stop();
            // Only close if mouse isn't over the popup
            if (_currentOpenPopup != null && !_currentOpenPopup.IsMouseOver)
            {
                CloseCurrentPopup();
            }
        };
        _popupCloseTimer.Start();
    }

    private void CloseCurrentPopup()
    {
        if (_currentOpenPopup != null)
        {
            _currentOpenPopup.IsOpen = false;
            _currentOpenPopup = null;
        }
    }

    private void PopupSafetyTimer_Tick(object? sender, EventArgs e)
    {
        // Safety check: close popup if neither button nor popup is hovered
        if (_currentOpenPopup != null && _currentOpenPopup.IsOpen)
        {
            var popupHovered = _currentOpenPopup.IsMouseOver;
            var buttonHovered = false;

            // Check if the associated button is still hovered
            if (_currentOpenPopup.PlacementTarget is System.Windows.Controls.Button button)
            {
                buttonHovered = button.IsMouseOver;
            }

            // If neither is hovered, close the popup
            if (!popupHovered && !buttonHovered)
            {
                _currentOpenPopup.IsOpen = false;
                _currentOpenPopup = null;
                _logger.LogDebug("Safety timer closed orphaned popup");
            }
        }
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // F2 to rename focused item
        if (e.Key == System.Windows.Input.Key.F2)
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel == null) return;

            // Get the focused element
            var focusedElement = System.Windows.Input.Keyboard.FocusedElement as FrameworkElement;
            if (focusedElement == null) return;

            // Find the data context of the focused element
            var dataContext = focusedElement.DataContext;

            if (dataContext is FolderShortcut shortcut)
            {
                viewModel.EditShortcutCommand.Execute(shortcut);
                e.Handled = true;
            }
            else if (dataContext is PowerShellCommand command)
            {
                viewModel.EditCommandCommand.Execute(command);
                e.Handled = true;
            }
            else if (dataContext is Project project)
            {
                viewModel.EditProjectCommand.Execute(project);
                e.Handled = true;
            }
        }
    }

    private void ReserveScreenSpace()
    {
        var workArea = new Rect();
        if (SystemParametersInfo(SpiGetworkarea, 0, ref workArea, 0))
        {
            // Reserve space on the left for sidebar
            var sidebarWidth = (int)Width;
            workArea.Left = sidebarWidth;

            if (SystemParametersInfo(SpiSetworkarea, 0, ref workArea, SpifUpdateinifile | SpifSendchange))
            {
                _logger.LogInformation("Reserved {Width}px screen space for sidebar", sidebarWidth);
            }
            else
            {
                _logger.LogWarning("Failed to reserve screen space for sidebar");
            }
        }
    }

    private void RestoreScreenSpace()
    {
        var workArea = new Rect();
        if (SystemParametersInfo(SpiGetworkarea, 0, ref workArea, 0))
        {
            // Restore original work area (remove left reservation)
            workArea.Left = 0;

            if (SystemParametersInfo(SpiSetworkarea, 0, ref workArea, SpifUpdateinifile | SpifSendchange))
            {
                _logger.LogInformation("Restored screen space");
            }
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Restore screen space when closing
        RestoreScreenSpace();
    }

    private System.Windows.Media.Color ParseRgbColor(string rgb, bool darkMode)
    {
        try
        {
            var parts = rgb.Split(',');
            if (parts.Length == 3 &&
                byte.TryParse(parts[0].Trim(), out byte r) &&
                byte.TryParse(parts[1].Trim(), out byte g) &&
                byte.TryParse(parts[2].Trim(), out byte b))
            {
                // Apply darkening if dark mode
                if (darkMode)
                {
                    r = (byte)(r * 0.7);
                    g = (byte)(g * 0.7);
                    b = (byte)(b * 0.7);
                }
                return System.Windows.Media.Color.FromRgb(r, g, b);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse RGB color: {Rgb}", rgb);
        }

        // Fallback to indigo
        return System.Windows.Media.Color.FromRgb(99, 102, 241);
    }
}