using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Serilog;
using WhisperVoice.Models;

namespace WhisperVoice.Services;

public sealed class JsonConfigService : IConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly object _lock = new();
    private readonly string _configPath;
    private AppConfig _current;

    public AppConfig Current => _current;
    public event EventHandler<AppConfig>? ConfigChanged;

    public JsonConfigService(string configPath)
    {
        _configPath = configPath;
        _current = Load();
    }

    public AppConfig Load()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                if (config is not null)
                {
                    _current = Sanitize(config);
                    return _current;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load config from {Path}, using defaults", _configPath);
        }

        _current = new AppConfig();
        return _current;
    }

    private static readonly HashSet<string> ValidModelNames =
        WhisperModelInfo.All.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ValidLanguageCodes =
        LanguageOption.All.Select(l => l.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static AppConfig Sanitize(AppConfig config)
    {
        var sanitizedModel = ValidModelNames.Contains(config.WhisperModel)
            ? config.WhisperModel
            : AppConstants.DefaultWhisperModel;

        var sanitizedLanguage = ValidLanguageCodes.Contains(config.Language)
            ? config.Language
            : AppConstants.DefaultLanguage;

        var sanitizedVkCode = config.Hotkey.VkCode is >= 0 and <= 255
            ? config.Hotkey.VkCode
            : AppConstants.DefaultHotkeyVkCode;

        if (sanitizedModel == config.WhisperModel
            && sanitizedVkCode == config.Hotkey.VkCode
            && sanitizedLanguage == config.Language)
            return config;

        return config with
        {
            WhisperModel = sanitizedModel,
            Language = sanitizedLanguage,
            Hotkey = config.Hotkey with { VkCode = sanitizedVkCode }
        };
    }

    public void Save(AppConfig config)
    {
        lock (_lock)
        {
            SaveInternal(config);
        }
    }

    public AppConfig Update(Func<AppConfig, AppConfig> updater)
    {
        lock (_lock)
        {
            var updated = updater(_current);
            SaveInternal(updated);
            return updated;
        }
    }

    private void SaveInternal(AppConfig config)
    {
        var dir = Path.GetDirectoryName(_configPath);
        if (dir is not null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configPath, json);
        _current = config;
        ConfigChanged?.Invoke(this, config);
    }
}
