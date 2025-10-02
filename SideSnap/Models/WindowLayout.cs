using System.Collections.ObjectModel;

namespace SideSnap.Models;

public class WindowLayout
{
    public string Name { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool ShowLabel { get; set; } = true;
    public LaunchBehavior LaunchBehavior { get; set; } = LaunchBehavior.LaunchIfNotRunning;
    public ObservableCollection<WindowPosition> Windows { get; set; } = [];
}

public enum LaunchBehavior
{
    AlwaysLaunch,           // Always launch new instances
    LaunchIfNotRunning,     // Only launch if not already running
    OnlyPosition            // Don't launch, only position existing windows
}
