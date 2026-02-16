using CommunityToolkit.Mvvm.ComponentModel;
using WhisperVoice.Services;

namespace WhisperVoice.ViewModels;

public partial class FloatingBarViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool _isRecording;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool _isProcessing;

    [ObservableProperty] private string _statusText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HotkeyInfo))]
    private string _hotkeyHint = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModelInfo))]
    private string _modelName = "";

    public bool IsIdle => !IsRecording && !IsProcessing;
    public string HotkeyInfo => string.Format(Strings.FloatingBarHotkeyInfoFormat, HotkeyHint);
    public string ModelInfo => string.Format(Strings.FloatingBarModelInfoFormat, ModelName);

    public FloatingBarViewModel(IConfigService config)
    {
        HotkeyHint = config.Current.HotkeyDisplay;
        ModelName = config.Current.WhisperModel;
        StatusText = string.Format(Strings.FloatingBarIdleFormat, HotkeyHint);
    }

    public void Sync(bool isRecording, bool isProcessing, string statusText)
    {
        IsRecording = isRecording;
        IsProcessing = isProcessing;
        StatusText = isRecording || isProcessing
            ? statusText
            : string.Format(Strings.FloatingBarIdleFormat, HotkeyHint);
    }
}
