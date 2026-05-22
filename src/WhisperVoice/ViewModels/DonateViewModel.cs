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
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WhisperVoice.Services;

namespace WhisperVoice.ViewModels;

public partial class DonateViewModel : ViewModelBase
{
    private readonly IConfigService _config;

    public bool ShouldShow => !_config.Current.HasDonated;

    public event EventHandler? DismissRequested;

    public DonateViewModel(IConfigService config)
    {
        _config = config;
    }

    [RelayCommand]
    private void Donate()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = AppConstants.DonateUrl,
            UseShellExecute = true
        });
        DismissRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void AlreadyDonated()
    {
        _config.Update(c => c with { HasDonated = true });
        DismissRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void MaybeLater()
    {
        // Just close - will show again next launch
        DismissRequested?.Invoke(this, EventArgs.Empty);
    }
}
