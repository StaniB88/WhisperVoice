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
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using WhisperVoice.Models;
using WhisperVoice.Services;

namespace WhisperVoice.ViewModels;

public partial class FloatingBarViewModel : ViewModelBase, IDisposable
{
    private readonly IConfigService _config;

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
        _config = config;
        HotkeyHint = config.Current.HotkeyDisplay;
        ModelName = config.Current.WhisperModel;
        StatusText = string.Format(Strings.FloatingBarIdleFormat, HotkeyHint);
        _config.ConfigChanged += OnConfigChanged;
    }

    private void OnConfigChanged(object? sender, AppConfig cfg)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (HotkeyHint != cfg.HotkeyDisplay)
            {
                HotkeyHint = cfg.HotkeyDisplay;
                if (IsIdle)
                    StatusText = string.Format(Strings.FloatingBarIdleFormat, HotkeyHint);
            }

            if (ModelName != cfg.WhisperModel)
                ModelName = cfg.WhisperModel;
        });
    }

    public void Sync(bool isRecording, bool isProcessing, string statusText)
    {
        IsRecording = isRecording;
        IsProcessing = isProcessing;
        StatusText = isRecording || isProcessing
            ? statusText
            : string.Format(Strings.FloatingBarIdleFormat, HotkeyHint);
    }

    public void Dispose()
    {
        _config.ConfigChanged -= OnConfigChanged;
    }
}
