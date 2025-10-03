using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Timers;
using Microsoft.Extensions.Logging;
using SideSnap.Models;

namespace SideSnap.Services;

public partial class WindowRuleService : IWindowRuleService
{
    private readonly string _rulesPath;
    private readonly IWindowManagerService _windowManager;
    private readonly ILogger<WindowRuleService> _logger;
    private System.Timers.Timer? _monitorTimer;
    private readonly HashSet<IntPtr> _processedWindows = [];
    private ObservableCollection<WindowRule> _rules = [];

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, char[] lpString, int nMaxCount);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public WindowRuleService(IWindowManagerService windowManager, ILogger<WindowRuleService> logger)
    {
        _windowManager = windowManager;
        _logger = logger;

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SideSnap");

        Directory.CreateDirectory(appDataPath);
        _rulesPath = Path.Combine(appDataPath, "window_rules.json");
    }

    public ObservableCollection<WindowRule> LoadRules()
    {
        try
        {
            if (!File.Exists(_rulesPath))
            {
                _logger.LogDebug("No rules file found, returning empty collection");
                return [];
            }

            var json = File.ReadAllText(_rulesPath);
            var rules = JsonSerializer.Deserialize<ObservableCollection<WindowRule>>(json) ?? [];
            _rules = rules;
            _logger.LogInformation("Loaded {Count} window rules", rules.Count);
            return rules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load window rules");
            return [];
        }
    }

    public void SaveRules(ObservableCollection<WindowRule> rules)
    {
        try
        {
            _rules = rules;
            var json = JsonSerializer.Serialize(rules, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_rulesPath, json);
            _logger.LogInformation("Saved {Count} window rules", rules.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save window rules");
        }
    }

    public bool MatchesRule(string processName, string windowTitle, WindowRule rule)
    {
        if (!rule.IsEnabled)
            return false;

        // Match process name (case-insensitive)
        if (!processName.Equals(rule.ProcessName, StringComparison.OrdinalIgnoreCase))
            return false;

        // Match window title if pattern is specified
        if (!string.IsNullOrEmpty(rule.WindowTitlePattern))
        {
            try
            {
                // Convert wildcards to regex pattern
                var pattern = "^" + Regex.Escape(rule.WindowTitlePattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";

                if (!Regex.IsMatch(windowTitle, pattern, RegexOptions.IgnoreCase))
                    return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid window title pattern: {Pattern}", rule.WindowTitlePattern);
                return false;
            }
        }

        return true;
    }

    public void StartMonitoring()
    {
        if (_monitorTimer != null)
            return;

        _logger.LogInformation("Starting window rule monitoring");
        _monitorTimer = new System.Timers.Timer(2000); // Check every 2 seconds
        _monitorTimer.Elapsed += MonitorTimer_Elapsed;
        _monitorTimer.AutoReset = true;
        _monitorTimer.Start();
    }

    public void StopMonitoring()
    {
        if (_monitorTimer == null)
            return;

        _logger.LogInformation("Stopping window rule monitoring");
        _monitorTimer.Stop();
        _monitorTimer.Dispose();
        _monitorTimer = null;
        _processedWindows.Clear();
    }

    private void MonitorTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            CheckWindows();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking windows for rules");
        }
    }

    private void CheckWindows()
    {
        var newWindows = new List<(IntPtr Handle, string ProcessName, string WindowTitle)>();

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            // Skip if already processed
            if (_processedWindows.Contains(hWnd))
                return true;

            GetWindowThreadProcessId(hWnd, out uint processId);
            try
            {
                var process = System.Diagnostics.Process.GetProcessById((int)processId);
                var title = GetWindowTitle(hWnd);

                if (!string.IsNullOrWhiteSpace(title))
                {
                    newWindows.Add((hWnd, process.ProcessName, title));
                }
            }
            catch { }

            return true;
        }, IntPtr.Zero);

        // Apply rules to new windows
        foreach (var (handle, processName, windowTitle) in newWindows)
        {
            foreach (var rule in _rules.Where(r => r.IsEnabled).OrderBy(r => r.Order))
            {
                if (MatchesRule(processName, windowTitle, rule))
                {
                    ApplyRule(handle, rule);
                    _processedWindows.Add(handle);
                    break; // Only apply first matching rule
                }
            }
        }
    }

    private void ApplyRule(IntPtr hwnd, WindowRule rule)
    {
        try
        {
            _logger.LogInformation("Applying rule '{Name}' to window", rule.Name);

            switch (rule.Action)
            {
                case RuleAction.SnapToZone when rule.SnapZone.HasValue:
                    // We need to temporarily set this window as foreground to snap it
                    // In a real implementation, you'd use SetForegroundWindow
                    _windowManager.SnapWindowToZone(rule.SnapZone.Value);
                    break;

                case RuleAction.Position when rule.CustomPosition != null:
                    _windowManager.MoveWindow(hwnd, rule.CustomPosition);
                    break;

                case RuleAction.Maximize:
                    _windowManager.MaximizeWindow(hwnd);
                    _logger.LogInformation("Maximized window for rule '{Name}'", rule.Name);
                    break;

                case RuleAction.Minimize:
                    _windowManager.MinimizeWindow(hwnd);
                    _logger.LogInformation("Minimized window for rule '{Name}'", rule.Name);
                    break;

                case RuleAction.Close:
                    _windowManager.CloseWindow(hwnd);
                    _logger.LogInformation("Closed window for rule '{Name}'", rule.Name);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply rule '{Name}'", rule.Name);
        }
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        const int maxLength = 256;
        var title = new char[maxLength];
        int length = GetWindowText(hWnd, title, maxLength);
        return length > 0 ? new string(title, 0, length) : string.Empty;
    }
}
