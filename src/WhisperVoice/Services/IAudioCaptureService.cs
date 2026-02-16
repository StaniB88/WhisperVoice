using System;
using System.IO;

namespace WhisperVoice.Services;

public interface IAudioCaptureService : IDisposable
{
    bool IsRecording { get; }
    void StartRecording();
    MemoryStream StopRecording();
}
