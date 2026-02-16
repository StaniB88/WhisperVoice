using System;
using WhisperVoice.Models;

namespace WhisperVoice.Services;

public interface IHotkeyService : IDisposable
{
    event EventHandler<HotkeyEventArgs>? HotkeyPressed;
    event EventHandler? HotkeyReleased;
    event EventHandler<HotkeyRecordedEventArgs>? HotkeyRecorded;

    void Start(HotkeyConfig config);
    void UpdateHotkey(HotkeyConfig config);
    void StartRecording();
    void CancelRecording();
}

public sealed class HotkeyEventArgs : EventArgs
{
    public IntPtr ForegroundWindow { get; init; }
}

public sealed class HotkeyRecordedEventArgs : EventArgs
{
    public required HotkeyConfig Config { get; init; }
    public required string Display { get; init; }
}
