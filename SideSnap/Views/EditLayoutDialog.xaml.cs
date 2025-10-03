using System.Windows;
using SideSnap.Models;

namespace SideSnap.Views;

public partial class EditLayoutDialog : Window
{
    public string LayoutName { get; private set; } = string.Empty;
    public string IconPath { get; private set; } = "🪟";
    public LaunchBehavior LaunchBehavior { get; private set; } = LaunchBehavior.OnlyPosition;

    public EditLayoutDialog()
    {
        InitializeComponent();
    }

    public EditLayoutDialog(WindowLayout layout) : this()
    {
        LayoutNameTextBox.Text = layout.Name;
        IconTextBox.Text = layout.IconPath;
        LaunchBehaviorComboBox.SelectedIndex = (int)layout.LaunchBehavior;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        LayoutName = LayoutNameTextBox.Text;
        IconPath = IconTextBox.Text;

        if (string.IsNullOrWhiteSpace(LayoutName))
        {
            System.Windows.MessageBox.Show("Please enter a layout name", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LaunchBehavior = LaunchBehaviorComboBox.SelectedIndex switch
        {
            0 => LaunchBehavior.OnlyPosition,
            1 => LaunchBehavior.LaunchIfNotRunning,
            2 => LaunchBehavior.AlwaysLaunch,
            _ => LaunchBehavior.OnlyPosition
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
