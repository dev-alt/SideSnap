using System.Collections.ObjectModel;

namespace SideSnap.Models;

public class WindowRule
{
    public string Name { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string? WindowTitlePattern { get; set; } // Optional: match window title (supports wildcards)
    public RuleAction Action { get; set; } = RuleAction.Position;
    public SnapZone? SnapZone { get; set; }
    public WindowPosition? CustomPosition { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Order { get; set; }
}

public enum RuleAction
{
    Position,      // Move to custom position
    SnapToZone,    // Snap to predefined zone
    Minimize,      // Minimize window
    Maximize,      // Maximize window
    Close          // Close window
}

public class WindowRuleMatch
{
    public IntPtr Handle { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public WindowRule Rule { get; set; } = null!;
}
