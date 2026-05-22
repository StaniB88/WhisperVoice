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
