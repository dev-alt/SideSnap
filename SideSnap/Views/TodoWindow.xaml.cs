using System.Windows;
using SideSnap.ViewModels;

namespace SideSnap.Views;

public partial class TodoWindow : Window
{
    public TodoWindow(TodoViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
