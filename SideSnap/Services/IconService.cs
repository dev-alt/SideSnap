using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace SideSnap.Services;

public class IconService : IIconService
{
    private readonly ILogger<IconService> _logger;
    private readonly Dictionary<string, ImageSource> _iconCache = new();

    // Win32 API
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref ShFileInfo psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct ShFileInfo
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint ShgfiIcon = 0x000000100;
    private const uint ShgfiSmallIcon = 0x000000001;
    private const uint ShgfiUseFileAttributes = 0x000000010;
    private const uint FileAttributeDirectory = 0x00000010;
    private const uint FileAttributeNormal = 0x00000080;

    public IconService(ILogger<IconService> logger)
    {
        _logger = logger;
    }

    public ImageSource GetIcon(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return GetFileIcon();

        // Check cache first
        if (_iconCache.TryGetValue(path, out var cachedIcon))
            return cachedIcon;

        try
        {
            ImageSource? icon = null;

            if (Directory.Exists(path))
            {
                icon = ExtractIcon(path, true);
            }
            else if (File.Exists(path))
            {
                var extension = Path.GetExtension(path).ToLower();
                if (extension == ".lnk")
                {
                    // For shortcuts, try to resolve the target
                    icon = ExtractIcon(path, false);
                }
                else if (extension == ".exe" || extension == ".ico")
                {
                    icon = ExtractIcon(path, false);
                }
                else
                {
                    icon = ExtractIcon(path, false);
                }
            }

            if (icon != null)
            {
                icon.Freeze(); // Make it thread-safe
                _iconCache[path] = icon;
                return icon;
            }

            _logger.LogDebug("Could not extract icon for: {Path}", path);
            return Directory.Exists(path) ? GetFolderIcon() : GetFileIcon();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get icon for: {Path}", path);
            return GetFileIcon();
        }
    }

    public ImageSource GetFolderIcon()
    {
        const string folderKey = "__folder__";
        if (_iconCache.TryGetValue(folderKey, out var icon))
            return icon;

        var folderIcon = ExtractGenericIcon(FileAttributeDirectory);
        if (folderIcon != null)
        {
            folderIcon.Freeze();
            _iconCache[folderKey] = folderIcon;
            return folderIcon;
        }

        return CreateDefaultIcon();
    }

    public ImageSource GetFileIcon()
    {
        const string fileKey = "__file__";
        if (_iconCache.TryGetValue(fileKey, out var icon))
            return icon;

        var fileIcon = ExtractGenericIcon(FileAttributeNormal);
        if (fileIcon != null)
        {
            fileIcon.Freeze();
            _iconCache[fileKey] = fileIcon;
            return fileIcon;
        }

        return CreateDefaultIcon();
    }

    private ImageSource? ExtractIcon(string path, bool isFolder)
    {
        var shinfo = new ShFileInfo();
        uint flags = ShgfiIcon | ShgfiSmallIcon;

        if (isFolder && !Directory.Exists(path))
        {
            flags |= ShgfiUseFileAttributes;
        }

        var result = SHGetFileInfo(
            path,
            isFolder ? FileAttributeDirectory : FileAttributeNormal,
            ref shinfo,
            (uint)Marshal.SizeOf(shinfo),
            flags);

        if (result == IntPtr.Zero)
            return null;

        try
        {
            if (shinfo.hIcon != IntPtr.Zero)
            {
                var icon = Imaging.CreateBitmapSourceFromHIcon(
                    shinfo.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                return icon;
            }
        }
        finally
        {
            if (shinfo.hIcon != IntPtr.Zero)
                DestroyIcon(shinfo.hIcon);
        }

        return null;
    }

    private ImageSource? ExtractGenericIcon(uint fileAttributes)
    {
        var shinfo = new ShFileInfo();
        var result = SHGetFileInfo(
            "",
            fileAttributes,
            ref shinfo,
            (uint)Marshal.SizeOf(shinfo),
            ShgfiIcon | ShgfiSmallIcon | ShgfiUseFileAttributes);

        if (result == IntPtr.Zero)
            return null;

        try
        {
            if (shinfo.hIcon != IntPtr.Zero)
            {
                return Imaging.CreateBitmapSourceFromHIcon(
                    shinfo.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
        }
        finally
        {
            if (shinfo.hIcon != IntPtr.Zero)
                DestroyIcon(shinfo.hIcon);
        }

        return null;
    }

    private ImageSource CreateDefaultIcon()
    {
        // Create a simple 16x16 placeholder icon
        var bitmap = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
        var visual = new System.Windows.Controls.Border
        {
            Width = 16,
            Height = 16,
            Background = System.Windows.Media.Brushes.LightGray
        };

        visual.Measure(new System.Windows.Size(16, 16));
        visual.Arrange(new Rect(0, 0, 16, 16));
        bitmap.Render(visual);
        bitmap.Freeze();

        return bitmap;
    }

    public void ClearCache()
    {
        _iconCache.Clear();
        _logger.LogInformation("Icon cache cleared");
    }
}