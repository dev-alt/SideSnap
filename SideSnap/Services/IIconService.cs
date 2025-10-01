using System.Windows.Media;

namespace SideSnap.Services;

public interface IIconService
{
    ImageSource? GetIcon(string path);
    ImageSource GetFolderIcon();
    ImageSource GetFileIcon();
}