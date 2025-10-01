namespace SideSnap.Services;

public class TrayService : ITrayService, IDisposable
{
    private NotifyIcon? _notifyIcon;
    private bool _disposed;

    public void Initialize()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "SideSnap",
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Show", null, OnShow);
        contextMenu.Items.Add("Hide", null, OnHide);
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, OnExit);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += OnDoubleClick;
    }

    public void ShowNotification(string title, string message)
    {
        _notifyIcon?.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
    }

    private void OnShow(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.MainWindow?.Show();
    }

    private void OnHide(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.MainWindow?.Hide();
    }

    private void OnExit(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void OnDoubleClick(object? sender, EventArgs e)
    {
        var mainWindow = System.Windows.Application.Current.MainWindow;
        if (mainWindow?.IsVisible == true)
        {
            mainWindow.Hide();
        }
        else
        {
            mainWindow?.Show();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _notifyIcon?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}