using System.Collections.Generic;
using Avalonia.Media;

namespace WhisperVoice.Models;

public sealed record ThemeOption(string Key, string DisplayName, string PreviewColor)
{
    public IBrush PreviewBrush => SolidColorBrush.Parse(PreviewColor);

    public static readonly IReadOnlyList<ThemeOption> All =
    [
        new("midnight", "Midnight", "#22d3ee"),
        new("obsidian", "Obsidian", "#60a5fa"),
        new("ocean", "Ocean", "#06b6d4"),
        new("crimson", "Crimson", "#fda4af"),
        new("emerald", "Emerald", "#6ee7b7"),
        new("lavender", "Lavender", "#a78bfa"),
    ];
}
