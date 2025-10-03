namespace SideSnap.Services;

public interface IStartupService
{
    /// <summary>
    /// Checks if the application is set to run on Windows startup
    /// </summary>
    bool IsStartupEnabled();

    /// <summary>
    /// Enables the application to run on Windows startup
    /// </summary>
    bool EnableStartup();

    /// <summary>
    /// Disables the application from running on Windows startup
    /// </summary>
    bool DisableStartup();
}
