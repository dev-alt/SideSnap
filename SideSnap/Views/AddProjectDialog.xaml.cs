using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SideSnap.Models;

namespace SideSnap.Views;

public partial class AddProjectDialog : Window
{
    public string ProjectName { get; private set; } = string.Empty;
    public string CustomIconPath { get; private set; } = string.Empty;
    public ObservableCollection<ProjectItem> Items { get; } = [];

    private Project? _editingProject;

    public AddProjectDialog()
    {
        InitializeComponent();
        ItemsListBox.ItemsSource = Items;
    }

    public void SetProject(Project project)
    {
        _editingProject = project;
        NameTextBox.Text = project.Name;
        IconPathTextBox.Text = project.IconPath;

        Items.Clear();
        foreach (var item in project.Items)
        {
            Items.Add(item);
        }
    }

    private void BrowseIconButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Icon",
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.ico;*.bmp)|*.png;*.jpg;*.jpeg;*.ico;*.bmp|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            IconPathTextBox.Text = dialog.FileName;
        }
    }

    private void AddFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select folder to add to project"
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var folderPath = dialog.SelectedPath;
            var folderName = Path.GetFileName(folderPath);

            Items.Add(new ProjectItem
            {
                Type = ProjectItemType.Folder,
                Name = folderName,
                Path = folderPath
            });
        }
    }

    private void AddScriptButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Script",
            Filter = "Script Files (*.sh;*.ps1;*.bat;*.cmd)|*.sh;*.ps1;*.bat;*.cmd|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            var scriptPath = dialog.FileName;
            var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
            var extension = Path.GetExtension(scriptPath).ToLower();

            string command;
            switch (extension)
            {
                case ".sh":
                    var wslPath = ConvertToWslPath(scriptPath);
                    command = $"wsl bash \"{wslPath}\"";
                    break;
                case ".ps1":
                    command = $"& '{scriptPath}'";
                    break;
                case ".bat":
                case ".cmd":
                    command = $"cmd /c \"{scriptPath}\"";
                    break;
                default:
                    command = scriptPath;
                    break;
            }

            Items.Add(new ProjectItem
            {
                Type = ProjectItemType.Script,
                Name = scriptName,
                Path = scriptPath,
                Command = command
            });
        }
    }

    private void AddCommandButton_Click(object sender, RoutedEventArgs e)
    {
        var inputDialog = new Window
        {
            Title = "Add Command",
            Width = 450,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize
        };

        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var nameLabel = new TextBlock { Text = "Command Name:", Margin = new Thickness(0, 0, 0, 5) };
        var nameBox = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 0, 0, 15), Padding = new Thickness(5) };

        var commandLabel = new TextBlock { Text = "Command:", Margin = new Thickness(0, 0, 0, 5) };
        var commandBox = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 0, 0, 15), Padding = new Thickness(5) };

        var buttonPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };

        var okButton = new System.Windows.Controls.Button
        {
            Content = "Add",
            Width = 80,
            Height = 30,
            Margin = new Thickness(0, 0, 10, 0)
        };

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "Cancel",
            Width = 80,
            Height = 30
        };

        okButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(nameBox.Text) && !string.IsNullOrWhiteSpace(commandBox.Text))
            {
                Items.Add(new ProjectItem
                {
                    Type = ProjectItemType.Command,
                    Name = nameBox.Text,
                    Command = commandBox.Text
                });
                inputDialog.DialogResult = true;
                inputDialog.Close();
            }
            else
            {
                System.Windows.MessageBox.Show("Please enter both a name and command.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };

        cancelButton.Click += (_, _) =>
        {
            inputDialog.DialogResult = false;
            inputDialog.Close();
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        Grid.SetRow(nameLabel, 0);
        Grid.SetRow(nameBox, 1);
        Grid.SetRow(commandLabel, 2);
        Grid.SetRow(commandBox, 3);
        Grid.SetRow(buttonPanel, 5);

        grid.Children.Add(nameLabel);
        grid.Children.Add(nameBox);
        grid.Children.Add(commandLabel);
        grid.Children.Add(commandBox);
        grid.Children.Add(buttonPanel);

        inputDialog.Content = grid;
        inputDialog.ShowDialog();
    }

    private void EditItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is ProjectItem item)
        {
            var inputDialog = new Window
            {
                Title = $"Edit {item.Type}",
                Width = 450,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var nameLabel = new TextBlock { Text = "Name:", Margin = new Thickness(0, 0, 0, 5) };
            var nameBox = new System.Windows.Controls.TextBox
            {
                Text = item.Name,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(5)
            };

            var buttonPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            var okButton = new System.Windows.Controls.Button
            {
                Content = "Save",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 30
            };

            okButton.Click += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(nameBox.Text))
                {
                    item.Name = nameBox.Text;
                    // Force refresh of the ListBox
                    var index = Items.IndexOf(item);
                    Items.RemoveAt(index);
                    Items.Insert(index, item);
                    inputDialog.DialogResult = true;
                    inputDialog.Close();
                }
                else
                {
                    System.Windows.MessageBox.Show("Please enter a name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            cancelButton.Click += (_, _) =>
            {
                inputDialog.DialogResult = false;
                inputDialog.Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(nameLabel, 0);
            Grid.SetRow(nameBox, 1);
            Grid.SetRow(buttonPanel, 3);

            grid.Children.Add(nameLabel);
            grid.Children.Add(nameBox);
            grid.Children.Add(buttonPanel);

            inputDialog.Content = grid;
            inputDialog.ShowDialog();
        }
    }

    private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is ProjectItem item)
        {
            Items.Remove(item);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            System.Windows.MessageBox.Show("Please enter a project name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ProjectName = NameTextBox.Text;
        CustomIconPath = IconPathTextBox.Text;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static string ConvertToWslPath(string windowsPath)
    {
        if (windowsPath.StartsWith(@"\\wsl.localhost\", StringComparison.OrdinalIgnoreCase) ||
            windowsPath.StartsWith(@"\\wsl$\", StringComparison.OrdinalIgnoreCase))
        {
            var parts = windowsPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                var pathPart = string.Join("/", parts.Skip(2));
                return "/" + pathPart;
            }
        }

        if (windowsPath.Length >= 2 && windowsPath[1] == ':')
        {
            var drive = char.ToLower(windowsPath[0]);
            var pathPart = windowsPath.Substring(2).Replace('\\', '/');
            return $"/mnt/{drive}{pathPart}";
        }

        return windowsPath.Replace('\\', '/');
    }
}
