using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace SideSnap.Utils;

public class FolderPreviewConverter : IValueConverter
{
    private const int MaxPreviewItems = 10;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || !Directory.Exists(path))
        {
            return "Folder not found";
        }

        try
        {
            var items = new List<string>();

            // Get directories first
            var directories = Directory.GetDirectories(path)
                .Take(MaxPreviewItems)
                .Select(d => $"📁 {Path.GetFileName(d)}");
            items.AddRange(directories);

            // Then get files
            var remainingSlots = MaxPreviewItems - items.Count;
            if (remainingSlots > 0)
            {
                var files = Directory.GetFiles(path)
                    .Take(remainingSlots)
                    .Select(f => $"📄 {Path.GetFileName(f)}");
                items.AddRange(files);
            }

            if (items.Count == 0)
            {
                return "Empty folder";
            }

            var totalItems = Directory.GetDirectories(path).Length + Directory.GetFiles(path).Length;
            var preview = string.Join("\n", items);

            if (totalItems > MaxPreviewItems)
            {
                preview += $"\n\n... and {totalItems - MaxPreviewItems} more items";
            }

            return preview;
        }
        catch (UnauthorizedAccessException)
        {
            return "Access denied";
        }
        catch (Exception)
        {
            return "Error reading folder";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
