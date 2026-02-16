using System;
using System.Threading.Tasks;

namespace WhisperVoice.Services;

public interface IClipboardService
{
    Task CopyTextAsync(string text);
    Task PasteAsync(IntPtr targetWindow = default);
}
