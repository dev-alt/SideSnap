using System.Collections.ObjectModel;
using SideSnap.Models;

namespace SideSnap.Services;

public interface ILayoutService
{
    ObservableCollection<WindowLayout> LoadLayouts();
    void SaveLayouts(ObservableCollection<WindowLayout> layouts);
    WindowLayout? GetLayoutByName(string name);
}
