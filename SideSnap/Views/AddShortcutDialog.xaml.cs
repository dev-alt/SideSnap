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
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select a folder",
            ShowNewFolderButton = true,
            SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            PathTextBox.Text = dialog.SelectedPath;
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                var trimmed = dialog.SelectedPath.TrimEnd(Path.DirectorySeparatorChar);
                var name = Path.GetFileName(trimmed);
                if (string.IsNullOrEmpty(name))
                {
                    name = trimmed;
                }
                NameTextBox.Text = name;
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