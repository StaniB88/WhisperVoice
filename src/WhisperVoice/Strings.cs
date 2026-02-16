namespace WhisperVoice;

/// <summary>
/// Centralized user-facing strings. Replace with resource file for localization.
/// </summary>
public static class Strings
{
    // Status
    public const string StatusReady = "Ready";
    public const string StatusRecording = "Recording...";
    public const string StatusProcessing = "Processing...";
    public const string StatusReadyWithHint = "Ready — press hotkey to dictate";

    // Settings - Model
    public const string ModelLoading = "Loading model...";
    public const string ModelLoadedFormat = "Model loaded ({0})";
    public const string ModelNotDownloadedFormat = "Not downloaded ({0})";
    public const string ModelNotFound = "Model file not found";
    public const string ModelDownloadComplete = "Download complete, loading model...";
    public const string ModelDownloadCancelled = "Download cancelled";

    // Settings - Download
    public const string DownloadingFormat = "Downloading {0}...";
    public const string DownloadingProgressFormat = "Downloading... {0:F0} MB";
    public const string DownloadingWithNameProgressFormat = "Downloading {0}... {1:F0} MB";

    // Settings - Hotkey
    public const string HotkeyRecordingPrompt = "Press a key combination...";

    // Setup
    public const string SetupChooseModel = "Choose a model to get started";
    public const string SetupDone = "Done!";

    // Floating Bar
    public const string FloatingBarHotkeyInfoFormat = "Hotkey: {0}";
    public const string FloatingBarModelInfoFormat = "Model: {0}";
    public const string FloatingBarIdleFormat = "{0} to dictate";

    // Errors
    public const string ErrorFormat = "Error: {0}";

    // Exceptions (developer-facing)
    public const string ExNotRecording = "Not recording";
    public const string ExModelNotLoaded = "Model not loaded. Call LoadModelAsync first.";
}
