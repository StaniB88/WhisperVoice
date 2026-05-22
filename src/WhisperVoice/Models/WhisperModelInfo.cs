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
using Whisper.net.Ggml;

namespace WhisperVoice.Models;

public sealed record WhisperModelInfo(
    string Name,
    string DisplayName,
    GgmlType GgmlType,
    string ApproxSize,
    string VramRequired,
    string Speed,
    string Quality,
    QuantizationType Quantization = QuantizationType.NoQuantization
)
{
    public static readonly IReadOnlyList<WhisperModelInfo> All =
    [
        new("tiny",                "Tiny",                  GgmlType.Tiny,          "~75 MB",  "~1 GB",  "~10x", "Basic"),
        new("tiny.en",             "Tiny (English)",        GgmlType.TinyEn,        "~75 MB",  "~1 GB",  "~10x", "Basic"),
        new("base",                "Base",                  GgmlType.Base,          "~142 MB", "~1 GB",  "~7x",  "Good"),
        new("base.en",             "Base (English)",        GgmlType.BaseEn,        "~142 MB", "~1 GB",  "~7x",  "Good"),
        new("small",               "Small",                 GgmlType.Small,         "~466 MB", "~2 GB",  "~4x",  "Better"),
        new("small.en",            "Small (English)",       GgmlType.SmallEn,       "~466 MB", "~2 GB",  "~4x",  "Better"),
        new("medium",              "Medium",                GgmlType.Medium,        "~1.5 GB", "~5 GB",  "~2x",  "Great"),
        new("medium.en",           "Medium (English)",      GgmlType.MediumEn,      "~1.5 GB", "~5 GB",  "~2x",  "Great"),
        new("large-v3",            "Large",                 GgmlType.LargeV3,       "~2.9 GB", "~10 GB", "1x",   "Best"),
        new("large-v3-q5_0",       "Large (Q5_0)",          GgmlType.LargeV3,       "~1.1 GB", "~5 GB",  "~1.5x","Great",  QuantizationType.Q5_0),
        new("large-v3-turbo",      "Large Turbo",           GgmlType.LargeV3Turbo,  "~1.5 GB", "~6 GB",  "~8x",  "Best"),
        new("large-v3-turbo-q5_0", "Large Turbo (Q5_0)",    GgmlType.LargeV3Turbo,  "~574 MB", "~3 GB",  "~10x", "Great",  QuantizationType.Q5_0),
    ];
}
