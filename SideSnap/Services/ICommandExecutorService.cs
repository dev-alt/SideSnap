using SideSnap.Models;

namespace SideSnap.Services;

public interface ICommandExecutorService
{
    List<PowerShellCommand> GetCommands();
    void SaveCommands(IEnumerable<PowerShellCommand> commands);
    Task ExecuteAsync(PowerShellCommand command);
}