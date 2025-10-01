using System.Globalization;
using System.Windows.Data;
using SideSnap.Services;
using Binding = System.Windows.Data.Binding;

// ReSharper disable NullnessAnnotationConflictWithJetBrainsAnnotations

namespace SideSnap.Utils;

public class PathToIconConverter : IValueConverter
{
    private static IIconService? _iconService;

    public static void Initialize(IIconService iconService)
    {
        _iconService = iconService;
    }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (_iconService == null || value is not string path || string.IsNullOrWhiteSpace(path))
            return Binding.DoNothing;

        return _iconService.GetIcon(path);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}