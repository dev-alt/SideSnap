using System.Diagnostics;
using System.IO;
using System.Text.Json;
using SideSnap.Models;

namespace SideSnap.Services;

public class ShortcutService : IShortcutService
{
    private readonly string _shortcutsPath;

    public ShortcutService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "SideSnap");
        Directory.CreateDirectory(appFolder);
        _shortcutsPath = Path.Combine(appFolder, "shortcuts.json");
    }

    public List<FolderShortcut> GetShortcuts()
    {
        if (!File.Exists(_shortcutsPath))
        {
            return GetDefaultShortcuts();
        }

        try
        {
            var json = File.ReadAllText(_shortcutsPath);
            return JsonSerializer.Deserialize<List<FolderShortcut>>(json) ?? GetDefaultShortcuts();
        }
        catch
        {
            return GetDefaultShortcuts();
        }
    }

    public void SaveShortcuts(IEnumerable<FolderShortcut> shortcuts)
    {
        var json = JsonSerializer.Serialize(shortcuts, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_shortcutsPath, json);
    }

    public async Task OpenFolderAsync(string path)
    {
        if (Directory.Exists(path) || File.Exists(path))
        {
            await Task.Run(() =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = path,
                    UseShellExecute = true
                });
            });
        }
    }

    private List<FolderShortcut> GetDefaultShortcuts()
    {
        return
        [
            new()
        {
                Name = "Documents", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Order = 0
            },
            new()
            {
                Name = "Downloads",
                Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                Order = 1
            },
            new() { Name = "Desktop", Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Order = 2 }
        ];
    }
}