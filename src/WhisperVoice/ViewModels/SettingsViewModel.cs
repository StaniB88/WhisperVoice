using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WhisperVoice.Models;
using WhisperVoice.Services;
using ThemeManager = WhisperVoice.Services.ThemeManager;

namespace WhisperVoice.ViewModels;

public partial class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly IConfigService _config;
    private readonly IHotkeyService _hotkey;
    private readonly IModelManager _modelManager;
    private readonly IUpdateService _updateService;
    private readonly ITranscriptionService _transcription;

    [ObservableProperty] private string _currentModel = "";
    [ObservableProperty] private LanguageOption? _selectedLanguage;
    [ObservableProperty] private bool _toggleMode;
    [ObservableProperty] private bool _autoPaste = true;
    [ObservableProperty] private bool _showFloatingBar = true;
    [ObservableProperty] private ThemeOption? _selectedTheme;
    [ObservableProperty] private string _hotkeyDisplay = "";
    [ObservableProperty] private bool _isRecordingHotkey;
    [ObservableProperty] private string _activeDevice = AppConstants.DefaultActiveDevice;
    [ObservableProperty] private WhisperModelInfo? _selectedModel;
    [ObservableProperty] private string _modelStatus = "";
    [ObservableProperty] private bool _isLoadingModel;
    [ObservableProperty] private bool _isModelMissing;
    [ObservableProperty] private bool _isDownloadingModel;
    [ObservableProperty] private double _modelDownloadProgress;
    [ObservableProperty] private string _modelDownloadText = "";

    // Update-related properties
    [ObservableProperty] private bool _updateAvailable;
    [ObservableProperty] private string _updateVersion = "";
    [ObservableProperty] private bool _isDownloadingUpdate;
    [ObservableProperty] private int _updateDownloadProgress;
    [ObservableProperty] private string _currentVersion = "";

    private UpdateInfo? _pendingUpdate;

    public IReadOnlyList<WhisperModelInfo> AvailableModels => _modelManager.GetAvailableModels();
    public IReadOnlyList<LanguageOption> AvailableLanguages => LanguageOption.All;
    public IReadOnlyList<ThemeOption> AvailableThemes => ThemeOption.All;

    public SettingsViewModel(
        IConfigService config,
        IHotkeyService hotkey,
        IModelManager modelManager,
        IUpdateService updateService,
        ITranscriptionService transcription)
    {
        _config = config;
        _hotkey = hotkey;
        _modelManager = modelManager;
        _updateService = updateService;
        _transcription = transcription;

        CurrentVersion = updateService.CurrentVersion ?? AppConstants.DevVersionLabel;

        // Load current settings
        var current = _config.Current;
        CurrentModel = current.WhisperModel;
        SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == current.Language) ?? AvailableLanguages[0];
        ToggleMode = current.ToggleMode;
        AutoPaste = current.AutoPaste;
        ShowFloatingBar = current.ShowFloatingBar;
        HotkeyDisplay = current.HotkeyDisplay;
        SelectedModel = AvailableModels.FirstOrDefault(m => m.Name == current.WhisperModel);
        SelectedTheme = AvailableThemes.FirstOrDefault(t => t.Key == current.Theme) ?? AvailableThemes[0];

        _hotkey.HotkeyRecorded += OnHotkeyRecorded;
        _updateService.UpdateAvailable += OnUpdateAvailable;
    }

    partial void OnToggleModeChanged(bool value) =>
        _config.Update(c => c with { ToggleMode = value });

    partial void OnAutoPasteChanged(bool value) =>
        _config.Update(c => c with { AutoPaste = value });

    partial void OnSelectedThemeChanged(ThemeOption? value)
    {
        if (value is null) return;
        Log.Information("Theme changed to {Theme}", value.Key);
        ThemeManager.Apply(value.Key);
        _config.Update(c => c with { Theme = value.Key });
    }

    partial void OnShowFloatingBarChanged(bool value)
    {
        _config.Update(c => c with { ShowFloatingBar = value });
        if (Application.Current is App app)
        {
            if (value) app.ShowFloatingBar();
            else app.HideFloatingBar();
        }
    }

    partial void OnSelectedModelChanged(WhisperModelInfo? value)
    {
        if (value is null) return;
        Log.Information("Model changed to {Model}", value.Name);
        CurrentModel = value.Name;
        _config.Update(c => c with { WhisperModel = value.Name });
        _ = ReloadModelAsync(value.Name);
    }

    private async Task ReloadModelAsync(string modelName)
    {
        IsLoadingModel = true;
        IsModelMissing = false;
        ModelStatus = Strings.ModelLoading;
        try
        {
            var modelPath = _modelManager.GetModelPath(modelName);
            if (File.Exists(modelPath))
            {
                Log.Information("Reloading model {Model} from {Path}", modelName, modelPath);
                await _transcription.LoadModelAsync(modelPath);
                ActiveDevice = _transcription.ActiveDevice ?? AppConstants.DefaultActiveDevice;
                ModelStatus = string.Format(Strings.ModelLoadedFormat, ActiveDevice);
                Log.Information("Model reloaded, device: {Device}", ActiveDevice);
            }
            else
            {
                Log.Warning("Model file not found: {Path}", modelPath);
                IsModelMissing = true;
                var model = AvailableModels.FirstOrDefault(m => m.Name == modelName);
                ModelStatus = model is not null
                    ? string.Format(Strings.ModelNotDownloadedFormat, model.ApproxSize)
                    : Strings.ModelNotFound;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to reload model {Model}", modelName);
            ModelStatus = string.Format(Strings.ErrorFormat, ex.Message);
        }
        finally
        {
            IsLoadingModel = false;
        }
    }

    [RelayCommand]
    private async Task DownloadModelAsync(CancellationToken ct)
    {
        if (SelectedModel is null) return;

        IsDownloadingModel = true;
        IsModelMissing = false;
        ModelDownloadText = string.Format(Strings.DownloadingFormat, SelectedModel.DisplayName);
        ModelDownloadProgress = 0;

        try
        {
            var progress = new Progress<double>(bytes =>
            {
                ModelDownloadProgress = bytes;
                ModelDownloadText = string.Format(Strings.DownloadingProgressFormat, bytes / AppConstants.BytesPerMegabyte);
            });

            await _modelManager.DownloadModelAsync(SelectedModel, progress, ct);
            ModelDownloadText = Strings.ModelDownloadComplete;
            Log.Information("Model {Model} downloaded", SelectedModel.Name);

            _config.Update(c => c with
            {
                WhisperModel = SelectedModel.Name,
                ModelPath = _modelManager.GetModelPath(SelectedModel.Name)
            });

            await ReloadModelAsync(SelectedModel.Name);
        }
        catch (OperationCanceledException)
        {
            ModelDownloadText = Strings.ModelDownloadCancelled;
            IsModelMissing = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to download model {Model}", SelectedModel.Name);
            ModelDownloadText = string.Format(Strings.ErrorFormat, ex.Message);
            IsModelMissing = true;
        }
        finally
        {
            IsDownloadingModel = false;
        }
    }

    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (value is null) return;
        _config.Update(c => c with { Language = value.Code });
    }

    [RelayCommand]
    private void StartRecordingHotkey()
    {
        IsRecordingHotkey = true;
        HotkeyDisplay = Strings.HotkeyRecordingPrompt;
        _hotkey.StartRecording();
    }

    [RelayCommand]
    private void CancelRecordingHotkey()
    {
        IsRecordingHotkey = false;
        HotkeyDisplay = _config.Current.HotkeyDisplay;
        _hotkey.CancelRecording();
    }

    [RelayCommand]
    private async Task CheckForUpdateAsync(CancellationToken ct)
    {
        try
        {
            var update = await _updateService.CheckForUpdateAsync(ct);
            if (update is not null)
            {
                _pendingUpdate = update;
                UpdateAvailable = true;
                UpdateVersion = update.Version;
            }
        }
        catch (OperationCanceledException)
        {
            // User cancelled, ignore
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to check for updates");
        }
    }

    [RelayCommand]
    private async Task DownloadAndInstallUpdateAsync(CancellationToken ct)
    {
        if (_pendingUpdate is null) return;

        IsDownloadingUpdate = true;
        var progress = new Progress<int>(p => UpdateDownloadProgress = p);

        try
        {
            await _updateService.DownloadUpdateAsync(_pendingUpdate, progress, ct);
            _updateService.ApplyUpdateAndRestart(_pendingUpdate);
        }
        catch (OperationCanceledException)
        {
            Log.Information("Update download cancelled");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to download or install update");
        }
        finally
        {
            IsDownloadingUpdate = false;
        }
    }

    private void OnHotkeyRecorded(object? sender, HotkeyRecordedEventArgs e)
    {
        Log.Information("Hotkey recorded: {Display}", e.Display);
        IsRecordingHotkey = false;
        HotkeyDisplay = e.Display;
        _config.Update(c => c with
        {
            Hotkey = e.Config,
            HotkeyDisplay = e.Display
        });
        _hotkey.UpdateHotkey(e.Config);
    }

    private void OnUpdateAvailable(object? sender, UpdateInfo update)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _pendingUpdate = update;
            UpdateAvailable = true;
            UpdateVersion = update.Version;
        });
        Log.Information("Update notification received: {Version}", update.Version);
    }

    public void Dispose()
    {
        _hotkey.HotkeyRecorded -= OnHotkeyRecorded;
        _updateService.UpdateAvailable -= OnUpdateAvailable;
    }
}
