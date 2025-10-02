using SideSnap.Models;

namespace SideSnap.Services;

public interface IWindowManagerService
{
    List<WindowPosition> GetSavedPositions();
    void SavePosition(WindowPosition position);
    void RestorePosition(string processName);

    // New methods for layout system
    /// <summary>
    /// Gets all currently open application windows
    /// </summary>
    List<CapturedWindow> GetOpenWindows();

    /// <summary>
    /// Launches an application with optional arguments
    /// </summary>
    bool LaunchApplication(string path, string arguments = "");

    /// <summary>
    /// Moves and resizes a window to the specified position
    /// </summary>
    bool MoveWindow(IntPtr hwnd, WindowPosition position);

    /// <summary>
    /// Finds window handles by process name and optionally window title
    /// </summary>
    List<IntPtr> FindWindowsByProcess(string processName, string? windowTitle = null);

    /// <summary>
    /// Applies a complete window layout
    /// </summary>
    void ApplyLayout(WindowLayout layout);

    /// <summary>
    /// Gets the current position and state of a window
    /// </summary>
    WindowPosition? GetWindowPosition(IntPtr hwnd);

    /// <summary>
    /// Snaps the foreground window to a predefined zone
    /// </summary>
    bool SnapWindowToZone(SnapZone zone, int monitorIndex = 0);

    /// <summary>
    /// Gets the foreground window handle
    /// </summary>
    IntPtr GetForegroundWindow();
}

/// <summary>
/// Represents a captured window with its current state
/// </summary>
public class CapturedWindow
{
    public IntPtr Handle { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string ApplicationPath { get; set; } = string.Empty;
    public int MonitorIndex { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public WindowState State { get; set; }
}
