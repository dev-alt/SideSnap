using System.IO;
using System.Windows;

namespace SideSnap.Views;

public partial class AddShortcutDialog
{
    public string ShortcutName { get; private set; } = string.Empty;
    public string ShortcutPath { get; private set; } = string.Empty;
    public string CustomIconPath { get; private set; } = string.Empty;
    public bool ShowLabel { get; private set; } = true;

    // New: texts to customize dialog header and primary button
    public string HeaderText { get; set; } = "Add New Shortcut";
    public string PrimaryButtonText { get; set; } = "Add";

    public AddShortcutDialog()
    {
        InitializeComponent();
        DataContext = this; // allow simple bindings to dialog properties
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        dialog.Description = "Select a folder";
        dialog.ShowNewFolderButton = true;
        dialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

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

    private void BrowseIconButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Icon File",
            Filter = "Icon Files (*.ico;*.png;*.jpg;*.bmp)|*.ico;*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        if (dialog.ShowDialog() == true)
        {
            IconPathTextBox.Text = dialog.FileName;
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
        CustomIconPath = IconPathTextBox.Text ?? string.Empty;
        ShowLabel = ShowLabelCheckBox.IsChecked == true;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}