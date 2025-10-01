using SideSnap.Models;

namespace SideSnap.Services;

public interface IProjectService
{
    List<Project> GetProjects();
    void SaveProjects(IEnumerable<Project> projects);
    void AddProject(Project project);
    void RemoveProject(Project project);
    void AddItemToProject(Project project, ProjectItem item);
    void RemoveItemFromProject(Project project, ProjectItem item);
}
