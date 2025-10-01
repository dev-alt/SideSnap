using System.IO;
using System.Windows;

namespace SideSnap.Views;

public partial class AddShortcutDialog : Window
{
    public string ShortcutName { get; private set; } = string.Empty;
    public string ShortcutPath { get; private set; } = string.Empty;

    public AddShortcutDialog()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Folder or File",
            Filter = "All Files (*.*)|*.*",
            CheckFileExists = false,
            CheckPathExists = true
        };

        // Try to use folder browser instead
        using var folderDialog = new FolderBrowserDialog
        {
            Description = "Select a folder",
            ShowNewFolderButton = true
        };

        if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            PathTextBox.Text = folderDialog.SelectedPath;
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                NameTextBox.Text = Path.GetFileName(folderDialog.SelectedPath);
            }
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            System.Windows.MessageBox.Show("Please enter a name for the shortcut.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(PathTextBox.Text))
        {
            System.Windows.MessageBox.Show("Please enter a path for the shortcut.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!Directory.Exists(PathTextBox.Text) && !File.Exists(PathTextBox.Text))
        {
            System.Windows.MessageBox.Show("The specified path does not exist.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ShortcutName = NameTextBox.Text;
        ShortcutPath = PathTextBox.Text;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}