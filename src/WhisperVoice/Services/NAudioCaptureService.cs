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
