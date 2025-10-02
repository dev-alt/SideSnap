using System.Windows;
using SideSnap.Services;

namespace SideSnap.Views;

public partial class AddCommandDialog
{
    private readonly ISettingsService _settingsService;

    public string CommandName { get; private set; } = string.Empty;
    public string CommandText { get; private set; } = string.Empty;
    public string CustomIconPath { get; private set; } = string.Empty;
    public bool RunHidden { get; private set; } = true;
    public bool RequiresElevation { get; private set; }
    public bool IsFavorite { get; private set; }
    public bool ShowLabel { get; private set; } = true;

    public AddCommandDialog(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeComponent();

        // Set default from settings
        var settings = _settingsService.LoadSettings();
        ShowLabelCheckBox.IsChecked = settings.ShowLabelByDefault;
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
            System.Windows.MessageBox.Show("Please enter a name for the command.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(CommandTextBox.Text))
        {
            System.Windows.MessageBox.Show("Please enter a PowerShell command.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CommandName = NameTextBox.Text;
        CommandText = CommandTextBox.Text;
        CustomIconPath = IconPathTextBox.Text ?? string.Empty;
        RunHidden = RunHiddenCheckBox.IsChecked == true;
        RequiresElevation = RequiresElevationCheckBox.IsChecked == true;
        IsFavorite = IsFavoriteCheckBox.IsChecked == true;
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