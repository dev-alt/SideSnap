using System.Windows;

namespace SideSnap.Views;

public partial class AddCommandDialog
{
    public string CommandName { get; private set; } = string.Empty;
    public string CommandText { get; private set; } = string.Empty;
    public bool RunHidden { get; private set; } = true;
    public bool RequiresElevation { get; private set; }
    public bool IsFavorite { get; private set; }

    public AddCommandDialog()
    {
        InitializeComponent();
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
        RunHidden = RunHiddenCheckBox.IsChecked == true;
        RequiresElevation = RequiresElevationCheckBox.IsChecked == true;
        IsFavorite = IsFavoriteCheckBox.IsChecked == true;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}