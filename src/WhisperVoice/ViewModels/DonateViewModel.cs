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
