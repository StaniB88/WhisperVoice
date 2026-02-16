using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net.Ggml;
using WhisperVoice.Models;

namespace WhisperVoice.Services;

public sealed class ModelManager : IModelManager
{
    public string ModelsDirectory { get; }

    public ModelManager(string modelsDirectory)
    {
        ModelsDirectory = modelsDirectory;
        if (!Directory.Exists(modelsDirectory))
            Directory.CreateDirectory(modelsDirectory);
    }

    public bool IsModelDownloaded(string modelName) =>
        File.Exists(GetModelPath(modelName));

    public string GetModelPath(string modelName)
    {
        var safeName = Path.GetFileName(modelName);

        if (!string.Equals(safeName, modelName, StringComparison.Ordinal))
            throw new ArgumentException($"Invalid model name: {modelName}");

        var path = Path.Combine(ModelsDirectory,
            $"{AppConstants.ModelFilePrefix}{safeName}{AppConstants.ModelFileExtension}");

        var fullPath = Path.GetFullPath(path);
        var modelsFullPath = Path.GetFullPath(ModelsDirectory);

        if (!fullPath.StartsWith(modelsFullPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Invalid model name: {modelName}");

        return fullPath;
    }

    public IReadOnlyList<WhisperModelInfo> GetAvailableModels() =>
        WhisperModelInfo.All;

    public async Task DownloadModelAsync(WhisperModelInfo model, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var modelPath = GetModelPath(model.Name);

        if (File.Exists(modelPath))
            return;

        using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(model.GgmlType, cancellationToken: ct);
        await using var fileStream = File.Create(modelPath);

        var buffer = new byte[AppConstants.DownloadBufferSize];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await modelStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalRead += bytesRead;
            progress?.Report(totalRead);
        }
    }
}
