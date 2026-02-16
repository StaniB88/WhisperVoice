using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WhisperVoice.Models;

namespace WhisperVoice.Services;

public interface ITranscriptionService : IDisposable
{
    bool IsModelLoaded { get; }
    string? ActiveDevice { get; }
    Task LoadModelAsync(string modelPath, CancellationToken ct = default);
    Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string language = AppConstants.AutoDetectLanguage, CancellationToken ct = default);
}
