using System.Collections.ObjectModel;

namespace SideSnap.Models;

public class Project
{
    public string Name { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool ShowLabel { get; set; } = true;
    public ObservableCollection<ProjectItem> Items { get; set; } = [];
}

public class ProjectItem
{
    public ProjectItemType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public bool ShowLabel { get; set; } = true;
}

public enum ProjectItemType
{
    Folder,
    Script,
    Command
}
