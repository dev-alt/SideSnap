using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Extensions.Logging;
using SideSnap.Models;
using SideSnap.Services;

namespace SideSnap.Views;

public partial class ClipboardHistoryWindow : Window
{
    private readonly IClipboardService _clipboardService;
    private readonly ILogger<ClipboardHistoryWindow> _logger;
    private ObservableCollection<ClipboardItem> _history = [];

    public ClipboardHistoryWindow(IClipboardService clipboardService, ILogger<ClipboardHistoryWindow> logger)
    {
        _clipboardService = clipboardService;
        _logger = logger;

        InitializeComponent();
        LoadHistory();

        _clipboardService.ClipboardChanged += OnClipboardChanged;
    }

    private void LoadHistory()
    {
        _history = _clipboardService.LoadHistory();
        SortHistory();
        HistoryItemsControl.ItemsSource = _history;
        _logger.LogDebug("Loaded {Count} clipboard items", _history.Count);
    }

    private void SortHistory()
    {
        var pinned = _history.Where(h => h.IsPinned).OrderByDescending(h => h.CopiedAt).ToList();
        var unpinned = _history.Where(h => !h.IsPinned).OrderByDescending(h => h.CopiedAt).ToList();

        _history.Clear();
        foreach (var item in pinned.Concat(unpinned))
        {
            _history.Add(item);
        }
    }

    private void OnClipboardChanged(object? sender, ClipboardItem item)
    {
        Dispatcher.Invoke(() =>
        {
            // Check if item already exists (avoid duplicates)
            var existing = _history.FirstOrDefault(h => h.Content == item.Content && h.Type == item.Type);
            if (existing != null)
            {
                existing.CopiedAt = DateTime.Now;
            }
            else
            {
                _history.Insert(0, item);
            }

            SortHistory();
            _clipboardService.SaveHistory(_history);
        });
    }

    private void CopyItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is ClipboardItem item)
        {
            try
            {
                if (item.Type == ClipboardItemType.Text)
                {
                    System.Windows.Clipboard.SetText(item.Content);
                }
                else if (item.Type == ClipboardItemType.File)
                {
                    var files = item.Content.Split(", ");
                    var fileCollection = new System.Collections.Specialized.StringCollection();
                    fileCollection.AddRange(files);
                    System.Windows.Clipboard.SetFileDropList(fileCollection);
                }

                _logger.LogInformation("Copied item to clipboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy item to clipboard");
                System.Windows.MessageBox.Show("Failed to copy to clipboard", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void PinItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is ClipboardItem item)
        {
            item.IsPinned = !item.IsPinned;
            SortHistory();
            _clipboardService.SaveHistory(_history);
            _logger.LogInformation("Toggled pin for item");
        }
    }

    private void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is ClipboardItem item)
        {
            var result = System.Windows.MessageBox.Show("Delete this clipboard item?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _history.Remove(item);
                _clipboardService.SaveHistory(_history);
                _logger.LogInformation("Deleted clipboard item");
            }
        }
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            "Clear all clipboard history? Pinned items will be kept.",
            "Confirm Clear",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var pinned = _history.Where(h => h.IsPinned).ToList();
            _history.Clear();
            foreach (var item in pinned)
            {
                _history.Add(item);
            }
            _clipboardService.SaveHistory(_history);
            _logger.LogInformation("Cleared clipboard history");
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _clipboardService.ClipboardChanged -= OnClipboardChanged;
        base.OnClosed(e);
    }
}
