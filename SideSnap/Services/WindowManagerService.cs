using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using SideSnap.Models;

namespace SideSnap.Services;

public partial class WindowManagerService : IWindowManagerService
{
    private readonly string _positionsPath;

    // Win32 API imports
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [LibraryImport("user32.dll")]
    private static partial IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

    private const uint MonitorDefaultToNearest = 0x00000002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpShowWindow = 0x0040;

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Rect
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

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
            return [];

        try
        {
            var json = File.ReadAllText(_positionsPath);
            return JsonSerializer.Deserialize<List<WindowPosition>>(json) ?? [];
        }
        catch
        {
            return [];
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

        if (position is null)
            return;

        var hwnd = FindWindowByProcessName(processName);
        if (hwnd != IntPtr.Zero)
        {
            _ = SetWindowPos(hwnd, IntPtr.Zero, position.X, position.Y, position.Width, position.Height, SwpNoZOrder | SwpShowWindow);
        }
    }

    private IntPtr FindWindowByProcessName(string processName)
    {
        IntPtr foundWindow = IntPtr.Zero;

        _ = EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true; // Continue enumeration

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
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out Rect rect))
            return null;

        var monitorIndex = GetMonitorIndex(hwnd);
        return new WindowPosition
        {
            ProcessName = processName,
            X = rect.Left,
            Y = rect.Top,
            Width = rect.Width,
            Height = rect.Height,
            Monitor = monitorIndex
        };
    }

    private int GetMonitorIndex(IntPtr hwnd)
    {
        var hMonitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        var monitors = GetAllMonitors();

        for (int i = 0; i < monitors.Count; i++)
        {
            if (monitors[i].Handle == hMonitor)
                return i;
        }
        return 0;
    }

    private List<(IntPtr Handle, Rect Bounds)> GetAllMonitors()
    {
        var monitors = new List<(IntPtr, Rect)>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref Rect lprcMonitor, IntPtr _) =>
        {
            monitors.Add((hMonitor, lprcMonitor));
            return true;
        }, IntPtr.Zero);

        return monitors;
    }

    public List<(int Index, int X, int Y, int Width, int Height)> GetMonitorInfo()
    {
        var monitors = GetAllMonitors();
        return monitors
            .Select((m, index) => (index, m.Bounds.Left, m.Bounds.Top, m.Bounds.Width, m.Bounds.Height))
            .ToList();
    }

    public void MoveWindow(string processName, int x, int y, int? width = null, int? height = null)
    {
        var hwnd = FindWindowByProcessName(processName);
        if (hwnd == IntPtr.Zero)
            return;

        if (width.HasValue && height.HasValue)
        {
            _ = SetWindowPos(hwnd, IntPtr.Zero, x, y, width.Value, height.Value, SwpNoZOrder | SwpShowWindow);
        }
        else
        {
            _ = SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SwpNoSize | SwpNoZOrder | SwpShowWindow);
        }
    }
}