using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WhisperVoice.Platform;

namespace WhisperVoice.Services;

public sealed class ClipboardService : IClipboardService
{
    public Task CopyTextAsync(string text)
    {
        // Use Win32 clipboard directly so it works even when our window is not focused
        if (!Win32Interop.OpenClipboard(IntPtr.Zero))
        {
            Log.Warning("Failed to open clipboard");
            return Task.CompletedTask;
        }

        try
        {
            Win32Interop.EmptyClipboard();

            var bytes = (text.Length + 1) * 2; // UTF-16 + null terminator
            var hGlobal = Win32Interop.GlobalAlloc(Win32Interop.GMEM_MOVEABLE, (nuint)bytes);
            if (hGlobal == IntPtr.Zero)
            {
                Log.Warning("GlobalAlloc failed");
                return Task.CompletedTask;
            }

            var ptr = Win32Interop.GlobalLock(hGlobal);
            if (ptr == IntPtr.Zero)
            {
                Win32Interop.GlobalFree(hGlobal);
                Log.Warning("GlobalLock failed");
                return Task.CompletedTask;
            }

            try
            {
                Marshal.Copy(text.ToCharArray(), 0, ptr, text.Length);
                Marshal.WriteInt16(ptr + text.Length * 2, 0);
            }
            finally
            {
                Win32Interop.GlobalUnlock(hGlobal);
            }

            if (Win32Interop.SetClipboardData(Win32Interop.CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
            {
                Win32Interop.GlobalFree(hGlobal);
                Log.Warning("SetClipboardData failed");
            }
        }
        finally
        {
            Win32Interop.CloseClipboard();
        }

        return Task.CompletedTask;
    }

    public async Task PasteAsync(IntPtr targetWindow = default)
    {
        // Run on background thread to avoid UI thread interference
        await Task.Run(() =>
        {
            if (targetWindow != IntPtr.Zero)
            {
                ForceForeground(targetWindow);
                Thread.Sleep(AppConstants.ForegroundSettleDelayMs);
            }

            var fg = Win32Interop.GetForegroundWindow();
            Log.Debug("Paste: target={Target}, actual foreground={Foreground}, match={Match}",
                targetWindow, fg, targetWindow == fg);

            // Release ALL modifier keys first via keybd_event
            byte[] modifiers = [
                (byte)Win32Interop.VK_LWIN, (byte)Win32Interop.VK_RWIN,
                (byte)Win32Interop.VK_LCTRL, (byte)Win32Interop.VK_RCTRL,
                (byte)Win32Interop.VK_LSHIFT, (byte)Win32Interop.VK_RSHIFT,
                (byte)Win32Interop.VK_LALT, (byte)Win32Interop.VK_RALT
            ];
            foreach (var mod in modifiers)
                Win32Interop.keybd_event(mod, 0, Win32Interop.KEYEVENTF_KEYUP, UIntPtr.Zero);

            Thread.Sleep(AppConstants.ModifierReleaseDelayMs);

            // Send Ctrl+V via keybd_event
            Win32Interop.keybd_event((byte)Win32Interop.VK_CTRL, 0, 0, UIntPtr.Zero);
            Win32Interop.keybd_event((byte)Win32Interop.VK_V, 0, 0, UIntPtr.Zero);
            Win32Interop.keybd_event((byte)Win32Interop.VK_V, 0, Win32Interop.KEYEVENTF_KEYUP, UIntPtr.Zero);
            Win32Interop.keybd_event((byte)Win32Interop.VK_CTRL, 0, Win32Interop.KEYEVENTF_KEYUP, UIntPtr.Zero);

            Log.Debug("keybd_event Ctrl+V sent");
        });
    }

    private static void ForceForeground(IntPtr targetWindow)
    {
        var currentForeground = Win32Interop.GetForegroundWindow();

        if (currentForeground == targetWindow)
        {
            Log.Debug("Target window already in foreground");
            return;
        }

        // Allow the target process to set the foreground window
        Win32Interop.GetWindowThreadProcessId(targetWindow, out var targetPid);
        Win32Interop.AllowSetForegroundWindow(targetPid);

        // Attach the foreground window's thread to the target window's thread
        // This tricks Windows into allowing the focus change
        var foregroundThread = Win32Interop.GetWindowThreadProcessId(currentForeground, out _);
        var targetThread = Win32Interop.GetWindowThreadProcessId(targetWindow, out _);

        var attached = false;
        if (foregroundThread != targetThread)
        {
            attached = Win32Interop.AttachThreadInput(foregroundThread, targetThread, true);
        }

        Win32Interop.BringWindowToTop(targetWindow);
        Win32Interop.ShowWindow(targetWindow, Win32Interop.SW_RESTORE);
        var result = Win32Interop.SetForegroundWindow(targetWindow);

        if (attached)
        {
            Win32Interop.AttachThreadInput(foregroundThread, targetThread, false);
        }

        Log.Debug("ForceForeground: target={Target}, result={Result}, attached={Attached}",
            targetWindow, result, attached);
    }
}
