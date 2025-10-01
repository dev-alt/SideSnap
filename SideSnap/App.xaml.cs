using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SideSnap.Services;
using SideSnap.Utils;
using SideSnap.ViewModels;
using SideSnap.Views;

namespace SideSnap;

public partial class App
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Setup unhandled exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        logger.LogInformation("SideSnap application starting...");

        // Initialize icon converter
        PathToIconConverter.Initialize(_serviceProvider.GetRequiredService<IIconService>());
        logger.LogInformation("Icon converter initialized");

        // Initialize tray service
        var trayService = _serviceProvider.GetRequiredService<ITrayService>();
        trayService.Initialize();
        logger.LogInformation("Tray service initialized");

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        logger.LogInformation("Main window shown");
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Logging
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logPath = Path.Combine(appDataPath, "SideSnap", "Logs");
        Directory.CreateDirectory(logPath);

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
            builder.AddDebug();
            builder.AddFile(Path.Combine(logPath, "sidesnap-{Date}.log"));
        });

        // Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ICommandExecutorService, CommandExecutorService>();
        services.AddSingleton<IWindowManagerService, WindowManagerService>();
        services.AddSingleton<IShortcutService, ShortcutService>();
        services.AddSingleton<ITrayService, TrayService>();
        services.AddSingleton<IIconService, IconService>();
        services.AddSingleton<ITodoService, TodoService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<TodoViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<AddShortcutDialog>();
        services.AddTransient<AddCommandDialog>();
        services.AddTransient<TodoWindow>();
        services.AddTransient<AddTodoDialog>();
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var logger = _serviceProvider?.GetService<ILogger<App>>();
        logger?.LogCritical(e.ExceptionObject as Exception, "Unhandled exception occurred");
        System.Windows.MessageBox.Show($"A critical error occurred: {e.ExceptionObject}", "SideSnap Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var logger = _serviceProvider?.GetService<ILogger<App>>();
        logger?.LogError(e.Exception, "Unhandled dispatcher exception occurred");
        System.Windows.MessageBox.Show($"An error occurred: {e.Exception.Message}", "SideSnap Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}