using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WhisperVoice.Models;

public sealed record AppConfig
{
    public bool SetupComplete { get; init; }
    public string WhisperModel { get; init; } = AppConstants.DefaultWhisperModel;
    public string Language { get; init; } = AppConstants.DefaultLanguage;
    public string AppLanguage { get; init; } = AppConstants.DefaultLanguage;
    public HotkeyConfig Hotkey { get; init; } = new();
    public string HotkeyDisplay { get; init; } = AppConstants.DefaultHotkeyDisplay;
    public bool ToggleMode { get; init; }
    public bool AutoPaste { get; init; } = true;
    public bool ShowFloatingBar { get; init; } = true;
    public string Theme { get; init; } = AppConstants.DefaultTheme;
    public bool HasDonated { get; init; }
    public string? ModelPath { get; init; }
    public StatsConfig Stats { get; init; } = new();
    public List<NoteEntry> Notes { get; init; } = [];
}

public sealed record HotkeyConfig
{
    public bool Ctrl { get; init; }
    public bool Shift { get; init; }
    public bool Alt { get; init; }
    public bool Win { get; init; } = true;
    public string Key { get; init; } = AppConstants.DefaultHotkeyKey;
    public int VkCode { get; init; } = AppConstants.DefaultHotkeyVkCode;
}

public sealed record StatsConfig
{
    public int TotalRecordings { get; init; }
    public int TotalWords { get; init; }
}
