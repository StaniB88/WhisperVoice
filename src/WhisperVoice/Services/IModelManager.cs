using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WhisperVoice.Models;

namespace WhisperVoice.Services;

public interface IModelManager
{
    string ModelsDirectory { get; }
    bool IsModelDownloaded(string modelName);
    string GetModelPath(string modelName);
    Task DownloadModelAsync(WhisperModelInfo model, IProgress<double>? progress = null, CancellationToken ct = default);
    IReadOnlyList<WhisperModelInfo> GetAvailableModels();
}
