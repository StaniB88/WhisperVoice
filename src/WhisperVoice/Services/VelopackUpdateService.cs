using System;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace WhisperVoice.Services;

public sealed class VelopackUpdateService : IUpdateService
{
    private readonly UpdateManager _mgr;

    public event EventHandler<UpdateInfo>? UpdateAvailable;

    public bool IsInstalled => _mgr.IsInstalled;
    public string? CurrentVersion => _mgr.IsInstalled ? _mgr.CurrentVersion?.ToString() : null;
    public UpdateInfo? LatestUpdate { get; private set; }

    public VelopackUpdateService(string githubRepoUrl)
    {
        _mgr = new UpdateManager(
            new GithubSource(githubRepoUrl, null, false));
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        if (!_mgr.IsInstalled) return null;

        var update = await _mgr.CheckForUpdatesAsync();
        if (update is null) return null;

        var info = new UpdateInfo(
            update.TargetFullRelease.Version.ToString(),
            update
        );

        LatestUpdate = info;
        UpdateAvailable?.Invoke(this, info);

        return info;
    }

    public async Task DownloadUpdateAsync(UpdateInfo update, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        var veloUpdate = (Velopack.UpdateInfo)update.InternalUpdate;
        await _mgr.DownloadUpdatesAsync(veloUpdate, p => progress?.Report(p));
    }

    public void ApplyUpdateAndRestart(UpdateInfo update)
    {
        var veloUpdate = (Velopack.UpdateInfo)update.InternalUpdate;
        _mgr.ApplyUpdatesAndRestart(veloUpdate.TargetFullRelease);
    }
}
