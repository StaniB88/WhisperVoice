using System;

namespace WhisperVoice.Models;

public sealed record TranscriptionResult(
    string Text,
    TimeSpan Duration,
    string? DetectedLanguage
);
