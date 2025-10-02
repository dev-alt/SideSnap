using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SideSnap.Models;

namespace SideSnap.Services;

public class QuickNotesService : IQuickNotesService
{
    private readonly string _notesPath;
    private readonly ILogger<QuickNotesService> _logger;

    public QuickNotesService(ILogger<QuickNotesService> logger)
    {
        _logger = logger;

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SideSnap");

        Directory.CreateDirectory(appDataPath);
        _notesPath = Path.Combine(appDataPath, "quick_notes.json");
    }

    public ObservableCollection<QuickNote> LoadNotes()
    {
        try
        {
            if (!File.Exists(_notesPath))
            {
                return [];
            }

            var json = File.ReadAllText(_notesPath);
            var notes = JsonSerializer.Deserialize<ObservableCollection<QuickNote>>(json) ?? [];
            _logger.LogInformation("Loaded {Count} notes", notes.Count);
            return notes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load notes");
            return [];
        }
    }

    public void SaveNotes(ObservableCollection<QuickNote> notes)
    {
        try
        {
            var json = JsonSerializer.Serialize(notes, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_notesPath, json);
            _logger.LogInformation("Saved {Count} notes", notes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save notes");
        }
    }

    public QuickNote CreateNote(string content = "")
    {
        return new QuickNote
        {
            Content = content,
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now
        };
    }

    public void DeleteNote(string id)
    {
        _logger.LogInformation("Note deleted: {Id}", id);
    }
}
