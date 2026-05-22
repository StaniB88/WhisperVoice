// Copyright (C) 2026 AnyAutomation
//
// This file is part of WhisperVoice.
//
// WhisperVoice is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// WhisperVoice is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with WhisperVoice.  If not, see <https://www.gnu.org/licenses/>.

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
