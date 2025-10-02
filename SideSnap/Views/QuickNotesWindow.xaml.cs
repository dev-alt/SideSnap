using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Extensions.Logging;
using SideSnap.Models;
using SideSnap.Services;

namespace SideSnap.Views;

public partial class QuickNotesWindow : Window
{
    private readonly IQuickNotesService _notesService;
    private readonly ILogger<QuickNotesWindow> _logger;
    private ObservableCollection<QuickNote> _notes = [];

    public QuickNotesWindow(IQuickNotesService notesService, ILogger<QuickNotesWindow> logger)
    {
        _notesService = notesService;
        _logger = logger;

        InitializeComponent();
        LoadNotes();
    }

    private void LoadNotes()
    {
        _notes = _notesService.LoadNotes();
        NotesItemsControl.ItemsSource = _notes;
        _logger.LogDebug("Loaded {Count} notes", _notes.Count);
    }

    private void AddNote_Click(object sender, RoutedEventArgs e)
    {
        var note = _notesService.CreateNote();
        _notes.Insert(0, note);
        _notesService.SaveNotes(_notes);
        _logger.LogInformation("Created new note");
    }

    private void DeleteNote_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is QuickNote note)
        {
            var result = MessageBox.Show("Delete this note?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _notes.Remove(note);
                _notesService.SaveNotes(_notes);
                _logger.LogInformation("Deleted note");
            }
        }
    }

    private void PinNote_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is QuickNote note)
        {
            note.IsPinned = !note.IsPinned;
            _notesService.SaveNotes(_notes);

            // Re-sort: pinned first
            var pinned = _notes.Where(n => n.IsPinned).ToList();
            var unpinned = _notes.Where(n => !n.IsPinned).ToList();
            _notes.Clear();
            foreach (var n in pinned.Concat(unpinned))
            {
                _notes.Add(n);
            }
        }
    }

    private void NoteContent_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox textBox && textBox.Tag is QuickNote note)
        {
            note.ModifiedAt = DateTime.Now;
            _notesService.SaveNotes(_notes);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
