using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using WhisperVoice.Services;
using WhisperVoice.ViewModels;
using WhisperVoice.Views;
using ThemeManager = WhisperVoice.Services.ThemeManager;

namespace WhisperVoice;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // Config
        services.AddSingleton<IConfigService>(new JsonConfigService(AppConstants.ConfigFilePath));
        Log.Debug("Config path: {ConfigPath}", AppConstants.ConfigFilePath);

        // Models
        services.AddSingleton<IModelManager>(new ModelManager(AppConstants.ModelsDirectoryPath));

        // Services
        services.AddSingleton<ITranscriptionService, WhisperNetService>();
        services.AddSingleton<IAudioCaptureService, NAudioCaptureService>();
        services.AddSingleton<IHotkeyService, Win32HotkeyService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IUpdateService>(
            new VelopackUpdateService(AppConstants.GitHubRepoUrl));

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SetupViewModel>();
        services.AddSingleton<DonateViewModel>();
        services.AddSingleton(sp => new FloatingBarViewModel(sp.GetRequiredService<IConfigService>()));
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<NotesViewModel>();

        Services = services.BuildServiceProvider();
        Log.Information("DI container built");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.ShutdownRequested += OnShutdownRequested;
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

            var config = Services.GetRequiredService<IConfigService>();
            ThemeManager.Apply(config.Current.Theme);

            if (!config.Current.SetupComplete)
            {
                Log.Information("First run — showing setup window");
                var setupVm = Services.GetRequiredService<SetupViewModel>();
                var setupWindow = new SetupWindow { DataContext = setupVm };
                desktop.MainWindow = setupWindow;

                setupVm.SetupCompleted += (_, _) =>
                {
                    Log.Information("Setup completed, transitioning to main window");
                    ShowMainAppWindow(desktop, config);
                    setupWindow.Close();
                };
            }
            else
            {
                ShowMainAppWindow(desktop, config);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private FloatingBarWindow? _floatingBar;

    private void ShowMainAppWindow(IClassicDesktopStyleApplicationLifetime desktop, IConfigService config)
    {
        var mainVm = Services.GetRequiredService<MainViewModel>();
        var mainWindow = new MainWindow { DataContext = mainVm };
        desktop.MainWindow = mainWindow;

        mainWindow.Opened += async (_, _) =>
        {
            var donate = Services.GetRequiredService<DonateViewModel>();
            if (donate.ShouldShow)
            {
                await Task.Delay(AppConstants.DonateDialogDelayMs);
                var dialog = new DonateDialog { DataContext = donate };
                donate.DismissRequested += (_, _) => dialog.Close();
                await dialog.ShowDialog(mainWindow);
            }
        };

        mainWindow.Show();
        Log.Information("Main window shown");

        var floatingBarVm = Services.GetRequiredService<FloatingBarViewModel>();
        mainVm.FloatingBar = floatingBarVm;

        if (config.Current.ShowFloatingBar)
            ShowFloatingBar();

        var hotkey = Services.GetRequiredService<IHotkeyService>();
        hotkey.Start(config.Current.Hotkey);
        Log.Information("Hotkey service started with {HotkeyDisplay}", config.Current.HotkeyDisplay);

        _ = LoadModelAsync(config, mainVm);
        _ = CheckForUpdatesInBackgroundAsync();
    }

    public void ShowFloatingBar()
    {
        if (_floatingBar is not null) return;
        var vm = Services.GetRequiredService<FloatingBarViewModel>();
        _floatingBar = new FloatingBarWindow { DataContext = vm };
        _floatingBar.Show();
        Log.Debug("Floating bar shown");
    }

    public void HideFloatingBar()
    {
        _floatingBar?.Close();
        _floatingBar = null;
        Log.Debug("Floating bar hidden");
    }

    private static async Task LoadModelAsync(IConfigService config, MainViewModel mainVm)
    {
        try
        {
            var transcription = Services.GetRequiredService<ITranscriptionService>();
            var modelManager = Services.GetRequiredService<IModelManager>();
            var modelPath = modelManager.GetModelPath(config.Current.WhisperModel);

            if (File.Exists(modelPath))
            {
                Log.Information("Loading Whisper model {Model} from {Path}", config.Current.WhisperModel, modelPath);
                await transcription.LoadModelAsync(modelPath);
                var device = transcription.ActiveDevice ?? AppConstants.DefaultActiveDevice;
                mainVm.ActiveDevice = device;
                Log.Information("Model loaded successfully, device: {Device}", device);
            }
            else
            {
                Log.Warning("Model file not found: {Path}", modelPath);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load Whisper model");
        }
    }

    private static async Task CheckForUpdatesInBackgroundAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(AppConstants.UpdateCheckDelaySeconds));
            var updateService = Services.GetRequiredService<IUpdateService>();
            var update = await updateService.CheckForUpdateAsync();
            if (update is not null)
                Log.Information("Update available: {Version}", update.Version);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Update check failed");
        }
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        Log.Information("Shutdown requested, cleaning up");
        HideFloatingBar();

        if (Services is IDisposable disposable)
            disposable.Dispose();
    }

    private void TrayIcon_OnClicked(object? sender, EventArgs e) =>
        ShowMainWindow();

    private void ShowWindow_OnClick(object? sender, EventArgs e) =>
        ShowMainWindow();

    private void HideFloatingBar_OnClick(object? sender, EventArgs e) =>
        HideFloatingBar();

    private void QuitApp_OnClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void ShowMainWindow()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is not null)
        {
            desktop.MainWindow.Show();
            desktop.MainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
            desktop.MainWindow.Activate();
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
