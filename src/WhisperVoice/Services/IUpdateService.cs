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

namespace WhisperVoice.Services;

public interface IUpdateService
{
    event EventHandler<UpdateInfo>? UpdateAvailable;
    bool IsInstalled { get; }
    string? CurrentVersion { get; }
    UpdateInfo? LatestUpdate { get; }
    Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default);
    Task DownloadUpdateAsync(UpdateInfo update, IProgress<int>? progress = null, CancellationToken ct = default);
    void ApplyUpdateAndRestart(UpdateInfo update);
}

public sealed record UpdateInfo(
    string Version,
    object InternalUpdate
);
