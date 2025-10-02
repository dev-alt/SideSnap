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

        // Load auto-hide setting
        var settings = _settingsService.LoadSettings();
        _isAutoHideEnabled = settings.AutoHide;

        // Apply visual style
        ApplyVisualStyle(settings.Style);

        // Setup auto-hide timer
        _hideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1500)
        };
        _hideTimer.Tick += HideTimer_Tick;

        // Hook into window messages to prevent moving
        Loaded += MainWindow_Loaded;
        MouseEnter += MainWindow_MouseEnter;
        MouseLeave += MainWindow_MouseLeave;

        if (_isAutoHideEnabled)
        {
            _logger.LogInformation("Auto-hide enabled");
        }
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
        var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
        settingsWindow.Owner = this;
        if (settingsWindow.ShowDialog() == true)
        {
            // Reload settings
            var settings = _settingsService.LoadSettings();
            _isAutoHideEnabled = settings.AutoHide;
            ApplyVisualStyle(settings.Style);
            _logger.LogInformation("Settings updated - Auto-hide: {AutoHide}, Style: {Style}", _isAutoHideEnabled, settings.Style);

            if (!_isAutoHideEnabled && !_isExpanded)
            {
                ExpandSidebar();
            }
        }
    }

    private void ApplyVisualStyle(AppStyle style)
    {
        var border = this.FindName("MainBorder") as System.Windows.Controls.Border;
        if (border == null) return;

        switch (style)
        {
            case AppStyle.Solid:
                border.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
                border.Effect = new DropShadowEffect { ShadowDepth = 2, BlurRadius = 10, Opacity = 0.3 };
                _logger.LogDebug("Applied Solid style");
                break;

            case AppStyle.Glass:
                // Glass effect with blur
                var glassBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 240, 240, 240));
                border.Background = glassBrush;
                border.Effect = new BlurEffect { Radius = 0 };

                // Add inner glow for glass effect
                var glassEffect = new DropShadowEffect
                {
                    ShadowDepth = 0,
                    BlurRadius = 15,
                    Opacity = 0.4,
                    Color = Colors.White
                };
                border.Effect = glassEffect;
                _logger.LogDebug("Applied Glass style");
                break;

            case AppStyle.Acrylic:
                // Acrylic effect with transparency
                var acrylicBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 245, 245, 245));
                border.Background = acrylicBrush;

                var acrylicEffect = new DropShadowEffect
                {
                    ShadowDepth = 2,
                    BlurRadius = 20,
                    Opacity = 0.5,
                    Color = System.Windows.Media.Color.FromRgb(100, 100, 100)
                };
                border.Effect = acrylicEffect;
                _logger.LogDebug("Applied Acrylic style");
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

    private void ProjectButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.DataContext is Models.Project project)
        {
            // Find the popup in the visual tree
            var grid = button.Parent as System.Windows.Controls.Grid;
            if (grid != null)
            {
                var popup = grid.FindName("ProjectDropdown") as System.Windows.Controls.Primitives.Popup;
                if (popup != null)
                {
                    popup.IsOpen = true;
                }
            }
        }
    }

    private void ProjectButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // Delay closing to allow mouse to move to popup
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        timer.Tick += (s, args) =>
        {
            timer.Stop();
            if (sender is System.Windows.Controls.Button button)
            {
                var grid = button.Parent as System.Windows.Controls.Grid;
                if (grid != null)
                {
                    var popup = grid.FindName("ProjectDropdown") as System.Windows.Controls.Primitives.Popup;
                    if (popup != null && !popup.IsMouseOver)
                    {
                        popup.IsOpen = false;
                    }
                }
            }
        };
        timer.Start();
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
}