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
