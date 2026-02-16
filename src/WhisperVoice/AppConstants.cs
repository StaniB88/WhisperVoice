using System;
using System.IO;

namespace WhisperVoice;

public static class AppConstants
{
    // App identity
    public const string AppName = "Whisper Voice";
    public const string AppDataFolderName = "whisper-voice";
    public const string MutexName = "WhisperVoice-SingleInstance";
    public const string ModelFilePrefix = "ggml-";
    public const string ModelFileExtension = ".bin";

    // Paths
    public static string AppDataPath =>
        Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData), AppDataFolderName);

    public static string ConfigFilePath => Path.Combine(AppDataPath, "config.json");
    public static string ModelsDirectoryPath => Path.Combine(AppDataPath, "models");
    public static string LogFilePath => Path.Combine(AppDataPath, "logs", "whispervoice-.log");

    // Defaults
    public const string DefaultWhisperModel = "base";
    public const string DefaultLanguage = "de";
    public const string DefaultTheme = "midnight";
    public const string DefaultHotkeyDisplay = "Win+D";
    public const string DefaultHotkeyKey = "D";
    public const int DefaultHotkeyVkCode = 0x44;
    public const string DefaultActiveDevice = "CPU";
    public const string DevVersionLabel = "dev";

    // URLs
    public const string GitHubRepoUrl = "https://github.com/StaniB88/WhisperVoice";
    public const string DonateUrl = "https://buymeacoffee.com/anyautomation";

    // Timing
    public const int DonateDialogDelayMs = 500;
    public const int UpdateCheckDelaySeconds = 10;
    public const int ClipboardSettleDelayMs = 100;
    public const int ForegroundSettleDelayMs = 200;
    public const int ModifierReleaseDelayMs = 50;
    public const int ClipboardClearDelayMs = 1000;

    // Limits
    public const int MaxHistoryEntries = 200;

    // Audio
    public const int WhisperSampleRate = 16000;
    public const int WhisperBitsPerSample = 16;
    public const int WhisperChannels = 1;
    public const int AudioBufferMs = 50;

    // Transcription
    public const string AutoDetectLanguage = "auto";
    public const int DownloadBufferSize = 81920;
    public const long BytesPerMegabyte = 1024 * 1024;

    // Logging
    public const int LogRetainedFileDays = 7;
    public const string LogOutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
}
