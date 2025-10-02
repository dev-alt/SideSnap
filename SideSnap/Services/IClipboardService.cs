using System.Collections.ObjectModel;
using SideSnap.Models;

namespace SideSnap.Services;

public interface IClipboardService
{
    ObservableCollection<ClipboardItem> LoadHistory();
    void SaveHistory(ObservableCollection<ClipboardItem> history);
    void StartMonitoring();
    void StopMonitoring();
    event EventHandler<ClipboardItem>? ClipboardChanged;
}
