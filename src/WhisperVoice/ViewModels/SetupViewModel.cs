using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WhisperVoice.Models;
using WhisperVoice.Services;

namespace WhisperVoice.ViewModels;

public partial class SetupViewModel : ViewModelBase
{
    private readonly IModelManager _modelManager;
    private readonly IConfigService _config;

    [ObservableProperty] private WhisperModelInfo? _selectedModel;
    [ObservableProperty] private double _downloadProgress;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private string _statusText = Strings.SetupChooseModel;

    public IReadOnlyList<WhisperModelInfo> AvailableModels => _modelManager.GetAvailableModels();

    public event EventHandler? SetupCompleted;

    public SetupViewModel(IModelManager modelManager, IConfigService config)
    {
        _modelManager = modelManager;
        _config = config;
        SelectedModel = AvailableModels.FirstOrDefault(m => m.Name == _config.Current.WhisperModel);
    }

    [RelayCommand]
    private async Task DownloadAndContinueAsync(CancellationToken ct)
    {
        if (SelectedModel is null) return;

        IsDownloading = true;
        StatusText = string.Format(Strings.DownloadingFormat, SelectedModel.DisplayName);

        try
        {
            var progress = new Progress<double>(bytes =>
            {
                DownloadProgress = bytes;
                StatusText = string.Format(Strings.DownloadingWithNameProgressFormat, SelectedModel.DisplayName, bytes / AppConstants.BytesPerMegabyte);
            });

            await _modelManager.DownloadModelAsync(SelectedModel, progress, ct);

            _config.Update(c => c with
            {
                SetupComplete = true,
                WhisperModel = SelectedModel.Name,
                ModelPath = _modelManager.GetModelPath(SelectedModel.Name)
            });

            StatusText = Strings.SetupDone;
            SetupCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            StatusText = Strings.ModelDownloadCancelled;
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Strings.ErrorFormat, ex.Message);
        }
        finally
        {
            IsDownloading = false;
        }
    }
}
