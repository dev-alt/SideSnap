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

    // Layout system methods
    public List<CapturedWindow> GetOpenWindows()
    {
        var windows = new List<CapturedWindow>();

        _ = EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            GetWindowThreadProcessId(hWnd, out uint processId);
            try
            {
                var process = Process.GetProcessById((int)processId);
                if (GetWindowRect(hWnd, out Rect rect))
                {
                    var title = GetWindowTitle(hWnd);

                    // Skip windows without titles (likely background windows)
                    if (string.IsNullOrWhiteSpace(title))
                        return true;

                    windows.Add(new CapturedWindow
                    {
                        Handle = hWnd,
                        ProcessName = process.ProcessName,
                        WindowTitle = title,
                        ApplicationPath = process.MainModule?.FileName ?? string.Empty,
                        MonitorIndex = GetMonitorIndex(hWnd),
                        X = rect.Left,
                        Y = rect.Top,
                        Width = rect.Width,
                        Height = rect.Height,
                        State = Models.WindowState.Normal // TODO: Detect actual state
                    });
                }
            }
            catch
            {
                // Process may have exited
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    public bool LaunchApplication(string path, string arguments = "")
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = path,
                Arguments = arguments,
                UseShellExecute = true
            };
            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool MoveWindow(IntPtr hwnd, WindowPosition position)
    {
        if (hwnd == IntPtr.Zero)
            return false;

        return SetWindowPos(hwnd, IntPtr.Zero, position.X, position.Y,
            position.Width, position.Height, SwpNoZOrder | SwpShowWindow);
    }

    public List<IntPtr> FindWindowsByProcess(string processName, string? windowTitle = null)
    {
        var windows = new List<IntPtr>();

        _ = EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            GetWindowThreadProcessId(hWnd, out uint processId);
            try
            {
                var process = Process.GetProcessById((int)processId);
                if (process.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                {
                    if (windowTitle == null)
                    {
                        windows.Add(hWnd);
                    }
                    else
                    {
                        var title = GetWindowTitle(hWnd);
                        if (title.Contains(windowTitle, StringComparison.OrdinalIgnoreCase))
                        {
                            windows.Add(hWnd);
                        }
                    }
                }
            }
            catch
            {
                // Process may have exited
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    public void ApplyLayout(WindowLayout layout)
    {
        foreach (var windowPos in layout.Windows)
        {
            var handles = FindWindowsByProcess(windowPos.ProcessName, windowPos.WindowTitle);

            if (handles.Count == 0 && layout.LaunchBehavior != LaunchBehavior.OnlyPosition)
            {
                // Launch the application
                if (LaunchApplication(windowPos.ApplicationPath, windowPos.Arguments))
                {
                    // Wait a bit for the window to appear
                    System.Threading.Thread.Sleep(1000);
                    handles = FindWindowsByProcess(windowPos.ProcessName, windowPos.WindowTitle);
                }
            }

            // Move all found windows (or just launched one)
            foreach (var hwnd in handles)
            {
                MoveWindow(hwnd, windowPos);
            }
        }
    }

    public WindowPosition? GetWindowPosition(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out Rect rect))
            return null;

        GetWindowThreadProcessId(hwnd, out uint processId);
        try
        {
            var process = Process.GetProcessById((int)processId);
            return new WindowPosition
            {
                ProcessName = process.ProcessName,
                WindowTitle = GetWindowTitle(hwnd),
                ApplicationPath = process.MainModule?.FileName ?? string.Empty,
                X = rect.Left,
                Y = rect.Top,
                Width = rect.Width,
                Height = rect.Height,
                MonitorIndex = GetMonitorIndex(hwnd),
                State = Models.WindowState.Normal // TODO: Detect actual state
            };
        }
        catch
        {
            return null;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, char[] lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect WorkArea;
        public uint Flags;
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        const int maxLength = 256;
        var title = new char[maxLength];
        int length = GetWindowText(hWnd, title, maxLength);
        return length > 0 ? new string(title, 0, length) : string.Empty;
    }

    public IntPtr GetForegroundWindow() => GetForegroundWindow();

    public bool SnapWindowToZone(SnapZone zone, int monitorIndex = 0)
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return false;

        // Get monitor info
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf(typeof(MonitorInfo)) };

        if (!GetMonitorInfo(monitor, ref monitorInfo))
            return false;

        var workArea = monitorInfo.WorkArea;
        var workWidth = workArea.Width;
        var workHeight = workArea.Height;

        int x, y, width, height;

        switch (zone)
        {
            case SnapZone.LeftHalf:
                x = workArea.Left;
                y = workArea.Top;
                width = workWidth / 2;
                height = workHeight;
                break;

            case SnapZone.RightHalf:
                x = workArea.Left + workWidth / 2;
                y = workArea.Top;
                width = workWidth / 2;
                height = workHeight;
                break;

            case SnapZone.TopHalf:
                x = workArea.Left;
                y = workArea.Top;
                width = workWidth;
                height = workHeight / 2;
                break;

            case SnapZone.BottomHalf:
                x = workArea.Left;
                y = workArea.Top + workHeight / 2;
                width = workWidth;
                height = workHeight / 2;
                break;

            case SnapZone.TopLeft:
                x = workArea.Left;
                y = workArea.Top;
                width = workWidth / 2;
                height = workHeight / 2;
                break;

            case SnapZone.TopRight:
                x = workArea.Left + workWidth / 2;
                y = workArea.Top;
                width = workWidth / 2;
                height = workHeight / 2;
                break;

            case SnapZone.BottomLeft:
                x = workArea.Left;
                y = workArea.Top + workHeight / 2;
                width = workWidth / 2;
                height = workHeight / 2;
                break;

            case SnapZone.BottomRight:
                x = workArea.Left + workWidth / 2;
                y = workArea.Top + workHeight / 2;
                width = workWidth / 2;
                height = workHeight / 2;
                break;

            case SnapZone.Center:
                width = (int)(workWidth * 0.6);
                height = (int)(workHeight * 0.6);
                x = workArea.Left + (workWidth - width) / 2;
                y = workArea.Top + (workHeight - height) / 2;
                break;

            case SnapZone.Maximize:
                x = workArea.Left;
                y = workArea.Top;
                width = workWidth;
                height = workHeight;
                break;

            default:
                return false;
        }

        return SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height, SwpNoZOrder | SwpShowWindow);
    }
}