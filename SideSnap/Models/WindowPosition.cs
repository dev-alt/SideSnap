namespace SideSnap.Models;

public class WindowPosition
{
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string ApplicationPath { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Monitor { get; set; }  // Keep for backwards compatibility
    public int MonitorIndex { get; set; }  // New property, same as Monitor
    public WindowState State { get; set; } = WindowState.Normal;
    public string Arguments { get; set; } = string.Empty;
}

public enum WindowState
{
    Normal,
    Maximized,
    Minimized
}
