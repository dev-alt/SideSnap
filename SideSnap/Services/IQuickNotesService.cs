using System.Collections.ObjectModel;
using SideSnap.Models;

namespace SideSnap.Services;

public interface IQuickNotesService
{
    ObservableCollection<QuickNote> LoadNotes();
    void SaveNotes(ObservableCollection<QuickNote> notes);
    QuickNote CreateNote(string content = "");
    void DeleteNote(string id);
}
