using SideSnap.Models;

namespace SideSnap.Services;

public interface IShortcutService
{
    List<FolderShortcut> GetShortcuts();
    void SaveShortcuts(IEnumerable<FolderShortcut> shortcuts);
    Task OpenFolderAsync(string path);
}