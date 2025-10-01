namespace SideSnap.Services;

public interface ITrayService
{
    void Initialize();
    void ShowNotification(string title, string message);
    void Dispose();
}