using SideSnap.Models;

namespace SideSnap.Services;

public interface IWindowManagerService
{
    List<WindowPosition> GetSavedPositions();
    void SavePosition(WindowPosition position);
    void RestorePosition(string processName);
}