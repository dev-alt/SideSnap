namespace SideSnap.Models;

public class AppSettings
{
    public double SidebarWidth { get; set; } = 250;
    public double SidebarHeight { get; set; } = 0; // 0 means full screen height
    public bool AutoHide { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public bool DarkMode { get; set; } = false;
    public double Opacity { get; set; } = 0.95;
    public AppStyle Style { get; set; } = AppStyle.Solid;
    public bool ShowLabelByDefault { get; set; } = true;
    public bool IsLocked { get; set; } = true; // Lock position and size by default
}

public enum AppStyle
{
    Solid,
    Glass,
    Acrylic
}