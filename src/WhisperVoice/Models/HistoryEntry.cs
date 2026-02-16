using System;

namespace WhisperVoice.Models;

public sealed record HistoryEntry(
    string Text,
    DateTime Timestamp,
    TimeSpan Duration,
    string? Language
);
