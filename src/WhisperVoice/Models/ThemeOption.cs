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
