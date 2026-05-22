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
using System.IO;
using NAudio.Wave;

namespace WhisperVoice.Services;

public sealed class NAudioCaptureService : IAudioCaptureService
{
    private static readonly WaveFormat WhisperFormat = new(
        AppConstants.WhisperSampleRate, AppConstants.WhisperBitsPerSample, AppConstants.WhisperChannels);

    private readonly object _lock = new();
    private WaveInEvent? _waveIn;
    private MemoryStream? _memoryStream;
    private WaveFileWriter? _writer;

    public bool IsRecording => _waveIn != null;

    public void StartRecording()
    {
        _memoryStream = new MemoryStream();
        _writer = new WaveFileWriter(_memoryStream, WhisperFormat);

        _waveIn = new WaveInEvent
        {
            WaveFormat = WhisperFormat,
            BufferMilliseconds = AppConstants.AudioBufferMs
        };

        _waveIn.DataAvailable += (_, e) =>
        {
            lock (_lock)
            {
                _writer?.Write(e.Buffer, 0, e.BytesRecorded);
            }
        };

        _waveIn.StartRecording();
    }

    public MemoryStream StopRecording()
    {
        if (_waveIn is null || _memoryStream is null || _writer is null)
            throw new InvalidOperationException(Strings.ExNotRecording);

        _waveIn.StopRecording();
        _waveIn.Dispose();
        _waveIn = null;

        lock (_lock)
        {
            _writer.Flush();
            _writer = null;
        }

        _memoryStream.Position = 0;
        var result = _memoryStream;
        _memoryStream = null;

        return result;
    }

    public void Dispose()
    {
        _waveIn?.StopRecording();
        _waveIn?.Dispose();
        lock (_lock)
        {
            _writer?.Dispose();
        }
        _memoryStream?.Dispose();
    }
}
