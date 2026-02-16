using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WhisperVoice.Models;
using WhisperVoice.Platform;
using WhisperVoice.Services;

namespace WhisperVoice.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ITranscriptionService _transcription;
    private readonly IAudioCaptureService _audio;
    private readonly IClipboardService _clipboard;
    private readonly IConfigService _config;
    private readonly IHotkeyService _hotkey;
    private bool _isTranscribing;
    private IntPtr _targetWindow;

    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private string _statusText = Strings.StatusReady;
    [ObservableProperty] private string _lastTranscription = "";
    [ObservableProperty] private string _activeDevice = AppConstants.DefaultActiveDevice;
    [ObservableProperty] private string _hotkeyHint = "";
    [ObservableProperty] private int _totalRecordings;
    [ObservableProperty] private int _totalWords;
    [ObservableProperty] private string _currentModelName = "";
    [ObservableProperty] private string _appVersion = AppConstants.DevVersionLabel;

    public FloatingBarViewModel? FloatingBar { get; set; }
    public ObservableCollection<HistoryEntry> History { get; } = [];

    public MainViewModel(
        ITranscriptionService transcription,
        IAudioCaptureService audio,
        IClipboardService clipboard,
        IConfigService config,
        IHotkeyService hotkey,
        IUpdateService updateService)
    {
        _transcription = transcription;
        _audio = audio;
        _clipboard = clipboard;
        _config = config;
        _hotkey = hotkey;

        _appVersion = updateService.CurrentVersion ?? AppConstants.DevVersionLabel;
        _hotkeyHint = config.Current.HotkeyDisplay;
        _totalRecordings = config.Current.Stats.TotalRecordings;
        _totalWords = config.Current.Stats.TotalWords;
        _currentModelName = config.Current.WhisperModel;
        _hotkey.HotkeyPressed += OnHotkeyPressed;
        _hotkey.HotkeyReleased += OnHotkeyReleased;
    }

    [RelayCommand]
    private void MicButtonClick()
    {
        if (IsRecording || _isTranscribing)
            _ = StopAndTranscribeAsync();
        else
            StartRecording();
    }

    private void OnHotkeyPressed(object? sender, HotkeyEventArgs e)
    {
        var targetWindow = e.ForegroundWindow;
        Dispatcher.UIThread.Post(() =>
        {
            if (_config.Current.ToggleMode)
            {
                if (IsRecording) _ = StopAndTranscribeAsync();
                else StartRecording(targetWindow);
            }
            else
            {
                if (!IsRecording) StartRecording(targetWindow);
            }
        });
    }

    private void OnHotkeyReleased(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!_config.Current.ToggleMode && IsRecording)
                _ = StopAndTranscribeAsync();
        });
    }

    private void StartRecording(IntPtr targetWindow = default)
    {
        if (_isTranscribing) return;
        _targetWindow = targetWindow != IntPtr.Zero ? targetWindow : Win32Interop.GetForegroundWindow();
        Log.Information("Recording started, target window: {Handle}", _targetWindow);
        IsRecording = true;
        StatusText = Strings.StatusRecording;
        _audio.StartRecording();
        FloatingBar?.Sync(IsRecording, IsProcessing, StatusText);
    }

    private async Task StopAndTranscribeAsync()
    {
        if (_isTranscribing) return;
        _isTranscribing = true;

        Log.Information("Recording stopped, starting transcription");
        IsRecording = false;
        IsProcessing = true;
        StatusText = Strings.StatusProcessing;
        FloatingBar?.Sync(IsRecording, IsProcessing, StatusText);

        try
        {
            using var audioStream = _audio.StopRecording();
            Log.Debug("Audio captured, stream size: {Size} bytes", audioStream.Length);
            var result = await _transcription.TranscribeAsync(audioStream, _config.Current.Language);

            LastTranscription = result.Text;
            History.Insert(0, new HistoryEntry(result.Text, DateTime.Now, result.Duration, result.DetectedLanguage));
            while (History.Count > AppConstants.MaxHistoryEntries)
                History.RemoveAt(History.Count - 1);

            TotalRecordings++;
            TotalWords += result.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            _config.Update(c => c with
            {
                Stats = c.Stats with
                {
                    TotalRecordings = TotalRecordings,
                    TotalWords = TotalWords
                }
            });

            await _clipboard.CopyTextAsync(result.Text);
            Log.Debug("Text copied to clipboard");

            if (_config.Current.AutoPaste)
            {
                await Task.Delay(AppConstants.ClipboardSettleDelayMs);
                await _clipboard.PasteAsync(_targetWindow);
                Log.Debug("Auto-paste triggered to window: {Handle}", _targetWindow);

                await Task.Delay(AppConstants.ClipboardClearDelayMs);
                await _clipboard.CopyTextAsync("");
            }

            StatusText = Strings.StatusReadyWithHint;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Transcription failed");
            StatusText = string.Format(Strings.ErrorFormat, ex.Message);
        }
        finally
        {
            IsProcessing = false;
            _isTranscribing = false;
            FloatingBar?.Sync(IsRecording, IsProcessing, StatusText);
        }
    }

    public void Dispose()
    {
        _hotkey.HotkeyPressed -= OnHotkeyPressed;
        _hotkey.HotkeyReleased -= OnHotkeyReleased;
    }
}
