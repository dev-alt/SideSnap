using System.Collections.ObjectModel;
using SideSnap.Models;

namespace SideSnap.Services;

public interface IWindowRuleService
{
    ObservableCollection<WindowRule> LoadRules();
    void SaveRules(ObservableCollection<WindowRule> rules);
    bool MatchesRule(string processName, string windowTitle, WindowRule rule);
    void StartMonitoring();
    void StopMonitoring();
}
