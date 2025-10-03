using SideSnap.Models;

namespace SideSnap.Services;

public interface ISettingsService
{
    AppSettings LoadSettings();
    void SaveSettings(AppSettings settings);
    void UpdateLockState(bool isLocked);
}