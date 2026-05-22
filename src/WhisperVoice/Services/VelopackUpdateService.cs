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
