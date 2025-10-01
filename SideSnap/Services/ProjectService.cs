using System.IO;
using System.Text.Json;
using SideSnap.Models;

namespace SideSnap.Services;

public class ProjectService : IProjectService
{
    private readonly string _projectsPath;

    public ProjectService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "SideSnap");
        Directory.CreateDirectory(appFolder);
        _projectsPath = Path.Combine(appFolder, "projects.json");
    }

    public List<Project> GetProjects()
    {
        if (!File.Exists(_projectsPath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_projectsPath);
            return JsonSerializer.Deserialize<List<Project>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void SaveProjects(IEnumerable<Project> projects)
    {
        var json = JsonSerializer.Serialize(projects, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_projectsPath, json);
    }

    public void AddProject(Project project)
    {
        var projects = GetProjects();
        projects.Add(project);
        SaveProjects(projects);
    }

    public void RemoveProject(Project project)
    {
        var projects = GetProjects();
        projects.Remove(project);
        SaveProjects(projects);
    }

    public void AddItemToProject(Project project, ProjectItem item)
    {
        project.Items.Add(item);
        var projects = GetProjects();
        var existingProject = projects.FirstOrDefault(p => p.Name == project.Name);
        if (existingProject != null)
        {
            existingProject.Items = project.Items;
            SaveProjects(projects);
        }
    }

    public void RemoveItemFromProject(Project project, ProjectItem item)
    {
        project.Items.Remove(item);
        var projects = GetProjects();
        var existingProject = projects.FirstOrDefault(p => p.Name == project.Name);
        if (existingProject != null)
        {
            existingProject.Items = project.Items;
            SaveProjects(projects);
        }
    }
}
