namespace SideSnap.Models;

public class PowerShellCommand
{
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool RunHidden { get; set; } = true;
    public bool RequiresElevation { get; set; } = false;
    public bool IsFavorite { get; set; } = false;
    public bool ShowLabel { get; set; } = true;
    public ScriptType ScriptType { get; set; } = ScriptType.PowerShell;
}

public enum ScriptType
{
    PowerShell,
    Bash,
    Executable,
    Other
}