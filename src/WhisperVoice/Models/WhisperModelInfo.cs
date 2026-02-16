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
    string Quality
)
{
    public static readonly IReadOnlyList<WhisperModelInfo> All =
    [
        new("tiny",           "Tiny",              GgmlType.Tiny,          "~75 MB",  "~1 GB",  "~10x", "Basic"),
        new("tiny.en",        "Tiny (English)",    GgmlType.TinyEn,       "~75 MB",  "~1 GB",  "~10x", "Basic"),
        new("base",           "Base",              GgmlType.Base,          "~142 MB", "~1 GB",  "~7x",  "Good"),
        new("base.en",        "Base (English)",    GgmlType.BaseEn,       "~142 MB", "~1 GB",  "~7x",  "Good"),
        new("small",          "Small",             GgmlType.Small,         "~466 MB", "~2 GB",  "~4x",  "Better"),
        new("small.en",       "Small (English)",   GgmlType.SmallEn,      "~466 MB", "~2 GB",  "~4x",  "Better"),
        new("medium",         "Medium",            GgmlType.Medium,        "~1.5 GB", "~5 GB",  "~2x",  "Great"),
        new("medium.en",      "Medium (English)",  GgmlType.MediumEn,     "~1.5 GB", "~5 GB",  "~2x",  "Great"),
        new("large-v3",       "Large",             GgmlType.LargeV3,      "~2.9 GB", "~10 GB", "1x",   "Best"),
        new("large-v3-turbo", "Large Turbo",       GgmlType.LargeV3Turbo, "~1.5 GB", "~6 GB",  "~8x",  "Best"),
    ];
}
