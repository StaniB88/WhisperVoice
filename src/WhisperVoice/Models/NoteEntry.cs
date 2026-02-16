using System;

namespace WhisperVoice.Models;

public sealed record NoteEntry
{
    public long Id { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public string Text { get; init; } = "";
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime? EditedAt { get; init; }
}
