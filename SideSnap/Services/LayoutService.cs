using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using SideSnap.Models;

namespace SideSnap.Services;

public class LayoutService : ILayoutService
{
    private readonly string _layoutsPath;

    public LayoutService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "SideSnap");
        Directory.CreateDirectory(appFolder);
        _layoutsPath = Path.Combine(appFolder, "layouts.json");
    }

    public ObservableCollection<WindowLayout> LoadLayouts()
    {
        if (!File.Exists(_layoutsPath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_layoutsPath);
            var layouts = JsonSerializer.Deserialize<ObservableCollection<WindowLayout>>(json);
            return layouts ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void SaveLayouts(ObservableCollection<WindowLayout> layouts)
    {
        var json = JsonSerializer.Serialize(layouts, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_layoutsPath, json);
    }

    public WindowLayout? GetLayoutByName(string name)
    {
        var layouts = LoadLayouts();
        return layouts.FirstOrDefault(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
