using System.Windows;
using System.Windows.Controls;

namespace SideSnap.Views;

public partial class AddTodoDialog : Window
{
    public string TodoCategory { get; private set; } = "UI/UX Improvements";
    public string TodoDescription { get; private set; } = string.Empty;
    public int TodoPriority { get; private set; } = 2; // Medium by default

    public AddTodoDialog()
    {
        InitializeComponent();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        // Get category (either selected or typed)
        TodoCategory = CategoryComboBox.Text;
        TodoDescription = DescriptionTextBox.Text;

        if (string.IsNullOrWhiteSpace(TodoCategory) || string.IsNullOrWhiteSpace(TodoDescription))
        {
            MessageBox.Show("Please fill in all fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Determine priority
        if (HighPriorityRadio.IsChecked == true)
            TodoPriority = 1;
        else if (MediumPriorityRadio.IsChecked == true)
            TodoPriority = 2;
        else if (LowPriorityRadio.IsChecked == true)
            TodoPriority = 3;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
