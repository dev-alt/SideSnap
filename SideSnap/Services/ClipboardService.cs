using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using SideSnap.Models;

namespace SideSnap.Services;

public partial class ClipboardService : IClipboardService
{
    private readonly string _historyPath;
    private readonly ILogger<ClipboardService> _logger;
    private HwndSource? _hwndSource;
    private const int WmClipboardupdate = 0x031D;
    private string _lastClipboardText = string.Empty;
    private const int MaxHistoryItems = 100;

    public event EventHandler<ClipboardItem>? ClipboardChanged;

    public ClipboardService(ILogger<ClipboardService> logger)
    {
        _logger = logger;

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SideSnap");

        Directory.CreateDirectory(appDataPath);
        _historyPath = Path.Combine(appDataPath, "clipboard_history.json");
    }

    public ObservableCollection<ClipboardItem> LoadHistory()
    {
        try
        {
            if (!File.Exists(_historyPath))
            {
                return [];
            }

            var json = File.ReadAllText(_historyPath);
            var history = JsonSerializer.Deserialize<ObservableCollection<ClipboardItem>>(json) ?? [];
            _logger.LogInformation("Loaded {Count} clipboard items", history.Count);
            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load clipboard history");
            return [];
        }
    }

    public void SaveHistory(ObservableCollection<ClipboardItem> history)
    {
        try
        {
            // Keep only the most recent items
            while (history.Count > MaxHistoryItems)
            {
                var oldestUnpinned = history.Where(h => !h.IsPinned).OrderBy(h => h.CopiedAt).FirstOrDefault();
                if (oldestUnpinned != null)
                {
                    history.Remove(oldestUnpinned);
                }
                else
                {
                    break;
                }
            }

            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_historyPath, json);
            _logger.LogInformation("Saved {Count} clipboard items", history.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save clipboard history");
        }
    }

    public void StartMonitoring()
    {
        try
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    var helper = new WindowInteropHelper(mainWindow);
                    _hwndSource = HwndSource.FromHwnd(helper.Handle);
                    if (_hwndSource != null)
                    {
                        _hwndSource.AddHook(WndProc);
                        AddClipboardFormatListener(helper.Handle);
                        _logger.LogInformation("Started clipboard monitoring");
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start clipboard monitoring");
        }
    }

    public void StopMonitoring()
    {
        try
        {
            if (_hwndSource != null)
            {
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    var helper = new WindowInteropHelper(mainWindow);
                    RemoveClipboardFormatListener(helper.Handle);
                }
                _hwndSource.RemoveHook(WndProc);
                _logger.LogInformation("Stopped clipboard monitoring");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop clipboard monitoring");
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmClipboardupdate)
        {
            OnClipboardChanged();
        }
        return IntPtr.Zero;
    }

    private void OnClipboardChanged()
    {
        try
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (System.Windows.Clipboard.ContainsText())
                {
                    var text = System.Windows.Clipboard.GetText();

                    // Avoid duplicates
                    if (text == _lastClipboardText || string.IsNullOrWhiteSpace(text))
                    {
                        return;
                    }

                    _lastClipboardText = text;

                    var item = new ClipboardItem
                    {
                        Content = text.Length > 500 ? text.Substring(0, 500) + "..." : text,
                        Type = ClipboardItemType.Text,
                        CopiedAt = DateTime.Now
                    };

                    ClipboardChanged?.Invoke(this, item);
                    _logger.LogDebug("Clipboard changed: {Preview}", item.Content.Substring(0, Math.Min(50, item.Content.Length)));
                }
                else if (System.Windows.Clipboard.ContainsFileDropList())
                {
                    var files = System.Windows.Clipboard.GetFileDropList();
                    if (files.Count > 0)
                    {
                        var fileList = string.Join(", ", files.Cast<string>());
                        var item = new ClipboardItem
                        {
                            Content = fileList.Length > 500 ? fileList.Substring(0, 500) + "..." : fileList,
                            Type = ClipboardItemType.File,
                            CopiedAt = DateTime.Now
                        };

                        ClipboardChanged?.Invoke(this, item);
                        _logger.LogDebug("Clipboard changed: {FileCount} files", files.Count);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing clipboard change");
        }
    }

    [System.Runtime.InteropServices.LibraryImport("user32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static partial bool AddClipboardFormatListener(IntPtr hwnd);

    [System.Runtime.InteropServices.LibraryImport("user32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static partial bool RemoveClipboardFormatListener(IntPtr hwnd);
}
