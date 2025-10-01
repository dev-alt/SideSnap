using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using SideSnap.Models;

namespace SideSnap.Services;

public class WindowManagerService : IWindowManagerService
{
    private readonly string _positionsPath;

    // Win32 API imports
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_SHOWWINDOW = 0x0040;

    public WindowManagerService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "SideSnap");
        Directory.CreateDirectory(appFolder);
        _positionsPath = Path.Combine(appFolder, "positions.json");
    }

    public List<WindowPosition> GetSavedPositions()
    {
        if (!File.Exists(_positionsPath))
        {
            return new List<WindowPosition>();
        }

        try
        {
            var json = File.ReadAllText(_positionsPath);
            return JsonSerializer.Deserialize<List<WindowPosition>>(json) ?? new List<WindowPosition>();
        }
        catch
        {
            return new List<WindowPosition>();
        }
    }

    public void SavePosition(WindowPosition position)
    {
        var positions = GetSavedPositions();
        var existing = positions.FirstOrDefault(p => p.ProcessName == position.ProcessName);

        if (existing != null)
        {
            positions.Remove(existing);
        }

        positions.Add(position);

        var json = JsonSerializer.Serialize(positions, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_positionsPath, json);
    }

    public void RestorePosition(string processName)
    {
        var positions = GetSavedPositions();
        var position = positions.FirstOrDefault(p => p.ProcessName == processName);

        if (position != null)
        {
            var hwnd = FindWindowByProcessName(processName);
            if (hwnd != IntPtr.Zero)
            {
                SetWindowPos(hwnd, IntPtr.Zero, position.X, position.Y, position.Width, position.Height, SWP_NOZORDER | SWP_SHOWWINDOW);
            }
        }
    }

    private IntPtr FindWindowByProcessName(string processName)
    {
        IntPtr foundWindow = IntPtr.Zero;

        EnumWindows((hWnd, lParam) =>
        {
            if (IsWindowVisible(hWnd))
            {
                GetWindowThreadProcessId(hWnd, out uint processId);
                try
                {
                    var process = Process.GetProcessById((int)processId);
                    if (process.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundWindow = hWnd;
                        return false; // Stop enumeration
                    }
                }
                catch
                {
                    // Process may have exited
                }
            }
            return true; // Continue enumeration
        }, IntPtr.Zero);

        return foundWindow;
    }

    public IntPtr? GetWindowHandle(string processName)
    {
        var hwnd = FindWindowByProcessName(processName);
        return hwnd == IntPtr.Zero ? null : hwnd;
    }

    public WindowPosition? GetCurrentWindowPosition(string processName)
    {
        var hwnd = FindWindowByProcessName(processName);
        if (hwnd != IntPtr.Zero && GetWindowRect(hwnd, out RECT rect))
        {
            var monitorIndex = GetMonitorIndex(hwnd);
            return new WindowPosition
            {
                ProcessName = processName,
                X = rect.Left,
                Y = rect.Top,
                Width = rect.Right - rect.Left,
                Height = rect.Bottom - rect.Top,
                Monitor = monitorIndex
            };
        }
        return null;
    }

    private int GetMonitorIndex(IntPtr hwnd)
    {
        var hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        var monitors = GetAllMonitors();

        for (int i = 0; i < monitors.Count; i++)
        {
            if (monitors[i].Item1 == hMonitor)
            {
                return i;
            }
        }
        return 0;
    }

    private List<(IntPtr, RECT)> GetAllMonitors()
    {
        var monitors = new List<(IntPtr, RECT)>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (hMonitor, hdcMonitor, ref RECT lprcMonitor, dwData) =>
        {
            monitors.Add((hMonitor, lprcMonitor));
            return true;
        }, IntPtr.Zero);

        return monitors;
    }

    public List<(int Index, int X, int Y, int Width, int Height)> GetMonitorInfo()
    {
        var result = new List<(int, int, int, int, int)>();
        var monitors = GetAllMonitors();

        for (int i = 0; i < monitors.Count; i++)
        {
            var (hMonitor, rect) = monitors[i];
            result.Add((i, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top));
        }

        return result;
    }

    public void MoveWindow(string processName, int x, int y, int? width = null, int? height = null)
    {
        var hwnd = FindWindowByProcessName(processName);
        if (hwnd != IntPtr.Zero)
        {
            if (width.HasValue && height.HasValue)
            {
                SetWindowPos(hwnd, IntPtr.Zero, x, y, width.Value, height.Value, SWP_NOZORDER | SWP_SHOWWINDOW);
            }
            else
            {
                SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
            }
        }
    }
}