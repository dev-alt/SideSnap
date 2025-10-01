using System.Globalization;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using SideSnap.Services;

namespace SideSnap.Utils;

public class PathToIconConverter : IValueConverter
{
    private static IIconService? _iconService;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        _iconService = serviceProvider.GetRequiredService<IIconService>();
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (_iconService == null || value is not string path || string.IsNullOrWhiteSpace(path))
            return null;

        return _iconService.GetIcon(path);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}