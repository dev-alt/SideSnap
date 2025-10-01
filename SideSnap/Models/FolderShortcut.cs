namespace SideSnap.Models;

public class FolderShortcut
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool ShowLabel { get; set; } = true;
}