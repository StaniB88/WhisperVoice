using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace WhisperVoice.Services;

public static class ThemeManager
{
    public static void Apply(string themeKey)
    {
        var app = Application.Current;
        if (app is null) return;

        var colors = themeKey switch
        {
            "obsidian" => Obsidian,
            "ocean" => Ocean,
            "crimson" => Crimson,
            "emerald" => Emerald,
            "lavender" => Lavender,
            _ => Midnight,
        };

        var dict = new ResourceDictionary();
        foreach (var (key, hex) in colors)
        {
            if (key.EndsWith("Color"))
                dict[key] = Color.Parse(hex);
            else
                dict[key] = new SolidColorBrush(Color.Parse(hex));
        }

        app.Resources.MergedDictionaries.Clear();
        app.Resources.MergedDictionaries.Add(dict);
    }

    // ── Midnight (default) ─────────────────────────────────────────────
    private static readonly Dictionary<string, string> Midnight = new()
    {
        // Backgrounds
        ["BgPrimary"] = "#0f0f1a",
        ["BgSecondary"] = "#161625",
        ["BgTertiary"] = "#0c0c18",
        ["BgSurface"] = "#1a1a2e",
        ["BgAccent"] = "#1e3a5f",
        ["BgAccentHover"] = "#264b78",
        ["BgAccentDim"] = "#0a2440",
        ["BgHover"] = "#1e1e35",
        ["BgOverlay"] = "#18ffffff",
        ["BgOverlayBorder"] = "#22ffffff",
        ["BgFloating"] = "#DD0c0c18",
        ["BgAccentTint"] = "#110f3460",
        // Accents
        ["Highlight"] = "#e94560",
        ["AccentBlue"] = "#3b82f6",
        ["AccentCyan"] = "#22d3ee",
        ["CloseButton"] = "#c42b1c",
        ["Success"] = "#34d399",
        // Text
        ["TextPrimary"] = "#e8e8f0",
        ["TextSecondary"] = "#8888a0",
        ["TextMuted"] = "#5c5c78",
        ["TextDim"] = "#3e3e58",
        ["TextSubtle"] = "#2e2e48",
        ["TextStatusBar"] = "#5c5c78",
        ["TextWhite"] = "#f4f4fa",
        // Borders
        ["BorderSubtle"] = "#18ffffff",
        ["BorderActive"] = "#3b82f6",
        // Mic ring
        ["MicRingTintIdle"] = "#0622d3ee",
        ["MicRingTintRecording"] = "#08e94560",
        // Floating bar
        ["IdleDot"] = "#00b4d8",
        ["ProcessingAmber"] = "#ffaa00",
        // Raw colors
        ["HighlightColor"] = "#e94560",
        ["AccentCyanColor"] = "#22d3ee",
        ["BgAccentColor"] = "#1e3a5f",
        ["BgSecondaryColor"] = "#161625",
        ["BgPrimaryColor"] = "#0f0f1a",
        ["ShadowColor"] = "#000000",
    };

    // ── Obsidian (true dark / OLED) ────────────────────────────────────
    private static readonly Dictionary<string, string> Obsidian = new()
    {
        ["BgPrimary"] = "#0d0d0d",
        ["BgSecondary"] = "#171717",
        ["BgTertiary"] = "#0a0a0a",
        ["BgSurface"] = "#1f1f1f",
        ["BgAccent"] = "#1a365d",
        ["BgAccentHover"] = "#234981",
        ["BgAccentDim"] = "#0f2240",
        ["BgHover"] = "#262626",
        ["BgOverlay"] = "#18ffffff",
        ["BgOverlayBorder"] = "#22ffffff",
        ["BgFloating"] = "#DD0a0a0a",
        ["BgAccentTint"] = "#10193860",
        ["Highlight"] = "#ef4444",
        ["AccentBlue"] = "#3b82f6",
        ["AccentCyan"] = "#60a5fa",
        ["CloseButton"] = "#c42b1c",
        ["Success"] = "#22c55e",
        ["TextPrimary"] = "#e5e5e5",
        ["TextSecondary"] = "#a3a3a3",
        ["TextMuted"] = "#737373",
        ["TextDim"] = "#525252",
        ["TextSubtle"] = "#404040",
        ["TextStatusBar"] = "#737373",
        ["TextWhite"] = "#f5f5f5",
        ["BorderSubtle"] = "#18ffffff",
        ["BorderActive"] = "#3b82f6",
        ["MicRingTintIdle"] = "#0660a5fa",
        ["MicRingTintRecording"] = "#08ef4444",
        // Floating bar
        ["IdleDot"] = "#60a5fa",
        ["ProcessingAmber"] = "#f59e0b",
        ["HighlightColor"] = "#ef4444",
        ["AccentCyanColor"] = "#60a5fa",
        ["BgAccentColor"] = "#1a365d",
        ["BgSecondaryColor"] = "#171717",
        ["BgPrimaryColor"] = "#0d0d0d",
        ["ShadowColor"] = "#000000",
    };

    // ── Ocean (deep sea teal) ──────────────────────────────────────────
    private static readonly Dictionary<string, string> Ocean = new()
    {
        ["BgPrimary"] = "#0b1929",
        ["BgSecondary"] = "#122640",
        ["BgTertiary"] = "#081420",
        ["BgSurface"] = "#183050",
        ["BgAccent"] = "#0c4a6e",
        ["BgAccentHover"] = "#0e6593",
        ["BgAccentDim"] = "#073550",
        ["BgHover"] = "#1a3050",
        ["BgOverlay"] = "#18ffffff",
        ["BgOverlayBorder"] = "#22ffffff",
        ["BgFloating"] = "#DD081420",
        ["BgAccentTint"] = "#100c4a6e",
        ["Highlight"] = "#f97316",
        ["AccentBlue"] = "#0ea5e9",
        ["AccentCyan"] = "#22d3ee",
        ["CloseButton"] = "#c42b1c",
        ["Success"] = "#2dd4bf",
        ["TextPrimary"] = "#e2e8f0",
        ["TextSecondary"] = "#8ba5be",
        ["TextMuted"] = "#5a7a96",
        ["TextDim"] = "#3a5a76",
        ["TextSubtle"] = "#2a4a66",
        ["TextStatusBar"] = "#5a7a96",
        ["TextWhite"] = "#f1f5f9",
        ["BorderSubtle"] = "#18ffffff",
        ["BorderActive"] = "#0ea5e9",
        ["MicRingTintIdle"] = "#0622d3ee",
        ["MicRingTintRecording"] = "#08f97316",
        // Floating bar
        ["IdleDot"] = "#22d3ee",
        ["ProcessingAmber"] = "#fb923c",
        ["HighlightColor"] = "#f97316",
        ["AccentCyanColor"] = "#22d3ee",
        ["BgAccentColor"] = "#0c4a6e",
        ["BgSecondaryColor"] = "#122640",
        ["BgPrimaryColor"] = "#0b1929",
        ["ShadowColor"] = "#000000",
    };

    // ── Crimson (dark rose) ────────────────────────────────────────────
    private static readonly Dictionary<string, string> Crimson = new()
    {
        ["BgPrimary"] = "#150a10",
        ["BgSecondary"] = "#201520",
        ["BgTertiary"] = "#10080c",
        ["BgSurface"] = "#2a1a25",
        ["BgAccent"] = "#6b213f",
        ["BgAccentHover"] = "#892a52",
        ["BgAccentDim"] = "#4a1530",
        ["BgHover"] = "#2a1a28",
        ["BgOverlay"] = "#18ffffff",
        ["BgOverlayBorder"] = "#22ffffff",
        ["BgFloating"] = "#DD10080c",
        ["BgAccentTint"] = "#106b213f",
        ["Highlight"] = "#f43f5e",
        ["AccentBlue"] = "#fb7185",
        ["AccentCyan"] = "#fda4af",
        ["CloseButton"] = "#c42b1c",
        ["Success"] = "#34d399",
        ["TextPrimary"] = "#f0e4e8",
        ["TextSecondary"] = "#a08890",
        ["TextMuted"] = "#785868",
        ["TextDim"] = "#583848",
        ["TextSubtle"] = "#482838",
        ["TextStatusBar"] = "#785868",
        ["TextWhite"] = "#faf0f2",
        ["BorderSubtle"] = "#18ffffff",
        ["BorderActive"] = "#fb7185",
        ["MicRingTintIdle"] = "#06fda4af",
        ["MicRingTintRecording"] = "#08f43f5e",
        // Floating bar
        ["IdleDot"] = "#fda4af",
        ["ProcessingAmber"] = "#fbbf24",
        ["HighlightColor"] = "#f43f5e",
        ["AccentCyanColor"] = "#fda4af",
        ["BgAccentColor"] = "#6b213f",
        ["BgSecondaryColor"] = "#201520",
        ["BgPrimaryColor"] = "#150a10",
        ["ShadowColor"] = "#000000",
    };

    // ── Emerald (dark forest) ──────────────────────────────────────────
    private static readonly Dictionary<string, string> Emerald = new()
    {
        ["BgPrimary"] = "#0a150e",
        ["BgSecondary"] = "#142018",
        ["BgTertiary"] = "#080f0a",
        ["BgSurface"] = "#1a2a20",
        ["BgAccent"] = "#155e3a",
        ["BgAccentHover"] = "#1a784a",
        ["BgAccentDim"] = "#0e3d25",
        ["BgHover"] = "#1a2a22",
        ["BgOverlay"] = "#18ffffff",
        ["BgOverlayBorder"] = "#22ffffff",
        ["BgFloating"] = "#DD080f0a",
        ["BgAccentTint"] = "#10155e3a",
        ["Highlight"] = "#f59e0b",
        ["AccentBlue"] = "#34d399",
        ["AccentCyan"] = "#6ee7b7",
        ["CloseButton"] = "#c42b1c",
        ["Success"] = "#10b981",
        ["TextPrimary"] = "#e4f0e8",
        ["TextSecondary"] = "#88a090",
        ["TextMuted"] = "#587860",
        ["TextDim"] = "#385840",
        ["TextSubtle"] = "#284830",
        ["TextStatusBar"] = "#587860",
        ["TextWhite"] = "#f0faf2",
        ["BorderSubtle"] = "#18ffffff",
        ["BorderActive"] = "#34d399",
        ["MicRingTintIdle"] = "#066ee7b7",
        ["MicRingTintRecording"] = "#08f59e0b",
        // Floating bar
        ["IdleDot"] = "#6ee7b7",
        ["ProcessingAmber"] = "#fbbf24",
        ["HighlightColor"] = "#f59e0b",
        ["AccentCyanColor"] = "#6ee7b7",
        ["BgAccentColor"] = "#155e3a",
        ["BgSecondaryColor"] = "#142018",
        ["BgPrimaryColor"] = "#0a150e",
        ["ShadowColor"] = "#000000",
    };

    // ── Lavender (dark violet) ─────────────────────────────────────────
    private static readonly Dictionary<string, string> Lavender = new()
    {
        ["BgPrimary"] = "#110e18",
        ["BgSecondary"] = "#1a1522",
        ["BgTertiary"] = "#0c0a12",
        ["BgSurface"] = "#221c2e",
        ["BgAccent"] = "#4c2889",
        ["BgAccentHover"] = "#6434ad",
        ["BgAccentDim"] = "#331a60",
        ["BgHover"] = "#221c30",
        ["BgOverlay"] = "#18ffffff",
        ["BgOverlayBorder"] = "#22ffffff",
        ["BgFloating"] = "#DD0c0a12",
        ["BgAccentTint"] = "#104c2889",
        ["Highlight"] = "#ec4899",
        ["AccentBlue"] = "#8b5cf6",
        ["AccentCyan"] = "#a78bfa",
        ["CloseButton"] = "#c42b1c",
        ["Success"] = "#34d399",
        ["TextPrimary"] = "#ece8f0",
        ["TextSecondary"] = "#9888a8",
        ["TextMuted"] = "#6c5880",
        ["TextDim"] = "#4c3860",
        ["TextSubtle"] = "#3c2850",
        ["TextStatusBar"] = "#6c5880",
        ["TextWhite"] = "#f6f0fa",
        ["BorderSubtle"] = "#18ffffff",
        ["BorderActive"] = "#8b5cf6",
        ["MicRingTintIdle"] = "#06a78bfa",
        ["MicRingTintRecording"] = "#08ec4899",
        // Floating bar
        ["IdleDot"] = "#a78bfa",
        ["ProcessingAmber"] = "#f59e0b",
        ["HighlightColor"] = "#ec4899",
        ["AccentCyanColor"] = "#a78bfa",
        ["BgAccentColor"] = "#4c2889",
        ["BgSecondaryColor"] = "#1a1522",
        ["BgPrimaryColor"] = "#110e18",
        ["ShadowColor"] = "#000000",
    };
}
