using System;
using System.Threading;
using System.Threading.Tasks;

namespace WhisperVoice.Services;

public interface IUpdateService
{
    bool IsInstalled { get; }
    string? CurrentVersion { get; }
    Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default);
    Task DownloadUpdateAsync(UpdateInfo update, IProgress<int>? progress = null, CancellationToken ct = default);
    void ApplyUpdateAndRestart(UpdateInfo update);
}

public sealed record UpdateInfo(
    string Version,
    object InternalUpdate
);
