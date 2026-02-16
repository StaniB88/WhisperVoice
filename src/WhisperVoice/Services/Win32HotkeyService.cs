using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WhisperVoice.Models;
using WhisperVoice.Platform;

namespace WhisperVoice.Services;

public sealed class Win32HotkeyService : IHotkeyService
{
    private IntPtr _hookId = IntPtr.Zero;
    private Win32Interop.LowLevelKeyboardProc? _proc;

    // Modifier state
    private bool _winPressed;
    private bool _ctrlPressed;
    private bool _shiftPressed;
    private bool _altPressed;
    private bool _hotkeyActive;

    // Current hotkey config
    private int _hotkeyVk;
    private bool _requireWin = true;
    private bool _requireCtrl;
    private bool _requireShift;
    private bool _requireAlt;

    // Recording mode
    private bool _recordingMode;
    private bool _recordCtrl;
    private bool _recordShift;
    private bool _recordAlt;
    private bool _recordWin;

    public event EventHandler<HotkeyEventArgs>? HotkeyPressed;
    public event EventHandler? HotkeyReleased;
    public event EventHandler<HotkeyRecordedEventArgs>? HotkeyRecorded;

    public void Start(HotkeyConfig config)
    {
        UpdateHotkey(config);
        _proc = HookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookId = Win32Interop.SetWindowsHookExW(
            Win32Interop.WH_KEYBOARD_LL, _proc,
            Win32Interop.GetModuleHandle(curModule.ModuleName!), 0);
    }

    public void UpdateHotkey(HotkeyConfig config)
    {
        _requireCtrl = config.Ctrl;
        _requireShift = config.Shift;
        _requireAlt = config.Alt;
        _requireWin = config.Win;
        _hotkeyVk = config.VkCode;
    }

    public void StartRecording()
    {
        _recordingMode = true;
        _recordCtrl = false;
        _recordShift = false;
        _recordAlt = false;
        _recordWin = false;
    }

    public void CancelRecording()
    {
        _recordingMode = false;
    }

    public static string GetKeyName(int vkCode)
    {
        if (vkCode >= Win32Interop.VK_A && vkCode <= Win32Interop.VK_Z) return ((char)vkCode).ToString();
        if (vkCode >= Win32Interop.VK_0 && vkCode <= Win32Interop.VK_9) return ((char)vkCode).ToString();
        if (vkCode >= Win32Interop.VK_F1 && vkCode <= Win32Interop.VK_F12) return $"F{vkCode - Win32Interop.VK_F1_OFFSET}";
        if (vkCode == Win32Interop.VK_SPACE) return "Space";
        return $"0x{vkCode:X2}";
    }

    public static bool IsModifierKey(int vkCode) =>
        vkCode is Win32Interop.VK_LWIN or Win32Interop.VK_RWIN
            or Win32Interop.VK_LCTRL or Win32Interop.VK_RCTRL or Win32Interop.VK_CTRL
            or Win32Interop.VK_LSHIFT or Win32Interop.VK_RSHIFT or Win32Interop.VK_SHIFT
            or Win32Interop.VK_LALT or Win32Interop.VK_RALT or Win32Interop.VK_ALT;

    public static int GetVirtualKeyCode(string key) => key.ToUpperInvariant() switch
    {
        "SPACE" or " " => Win32Interop.VK_SPACE,
        _ when key.Length == 1 && char.IsLetterOrDigit(key[0]) => char.ToUpper(key[0]),
        _ => AppConstants.DefaultHotkeyVkCode
    };

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
            return Win32Interop.CallNextHookEx(_hookId, nCode, wParam, lParam);

        var hookStruct = Marshal.PtrToStructure<Win32Interop.KBDLLHOOKSTRUCT>(lParam);
        var vkCode = (int)hookStruct.vkCode;
        var msg = wParam.ToInt32();
        var isKeyDown = msg is Win32Interop.WM_KEYDOWN or Win32Interop.WM_SYSKEYDOWN;
        var isKeyUp = msg is Win32Interop.WM_KEYUP or Win32Interop.WM_SYSKEYUP;

        // Track modifier state
        TrackModifier(vkCode, isKeyDown);

        if (_recordingMode)
            return HandleRecordingMode(vkCode, isKeyDown, isKeyUp, nCode, wParam, lParam);

        return HandleHotkeyMode(vkCode, isKeyDown, isKeyUp, nCode, wParam, lParam);
    }

    private void TrackModifier(int vkCode, bool isKeyDown)
    {
        if (vkCode is Win32Interop.VK_LWIN or Win32Interop.VK_RWIN)
        {
            _winPressed = isKeyDown;
            if (_recordingMode && isKeyDown) _recordWin = true;
        }
        if (vkCode is Win32Interop.VK_LCTRL or Win32Interop.VK_RCTRL or Win32Interop.VK_CTRL)
        {
            _ctrlPressed = isKeyDown;
            if (_recordingMode && isKeyDown) _recordCtrl = true;
        }
        if (vkCode is Win32Interop.VK_LSHIFT or Win32Interop.VK_RSHIFT or Win32Interop.VK_SHIFT)
        {
            _shiftPressed = isKeyDown;
            if (_recordingMode && isKeyDown) _recordShift = true;
        }
        if (vkCode is Win32Interop.VK_LALT or Win32Interop.VK_RALT or Win32Interop.VK_ALT)
        {
            _altPressed = isKeyDown;
            if (_recordingMode && isKeyDown) _recordAlt = true;
        }
    }

    private IntPtr HandleRecordingMode(int vkCode, bool isKeyDown, bool isKeyUp, int nCode, IntPtr wParam, IntPtr lParam)
    {
        // Modifier + normal key
        if (isKeyDown && !IsModifierKey(vkCode) && (_recordCtrl || _recordShift || _recordAlt || _recordWin))
        {
            var display = BuildDisplayString(vkCode);
            var config = new HotkeyConfig
            {
                Ctrl = _recordCtrl,
                Shift = _recordShift,
                Alt = _recordAlt,
                Win = _recordWin,
                Key = GetKeyName(vkCode),
                VkCode = vkCode
            };
            HotkeyRecorded?.Invoke(this, new HotkeyRecordedEventArgs { Config = config, Display = display });
            _recordingMode = false;
            if (_recordWin) return (IntPtr)1;
        }

        // Modifier-only combo (e.g. Ctrl+Win)
        if (isKeyUp && IsModifierKey(vkCode))
        {
            var allReleased = !_ctrlPressed && !_shiftPressed && !_altPressed && !_winPressed;
            var modCount = (_recordCtrl ? 1 : 0) + (_recordShift ? 1 : 0) + (_recordAlt ? 1 : 0) + (_recordWin ? 1 : 0);
            if (allReleased && modCount >= 2)
            {
                var display = BuildDisplayString(0);
                var config = new HotkeyConfig
                {
                    Ctrl = _recordCtrl, Shift = _recordShift, Alt = _recordAlt, Win = _recordWin,
                    Key = "", VkCode = 0
                };
                HotkeyRecorded?.Invoke(this, new HotkeyRecordedEventArgs { Config = config, Display = display });
                _recordingMode = false;
            }
        }

        return Win32Interop.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private IntPtr HandleHotkeyMode(int vkCode, bool isKeyDown, bool isKeyUp, int nCode, IntPtr wParam, IntPtr lParam)
    {
        var isModifierOnly = _hotkeyVk == 0;

        if (isModifierOnly)
        {
            var allMods = ModifiersMatch();
            if (isKeyDown && IsModifierKey(vkCode) && allMods && !_hotkeyActive)
            {
                _hotkeyActive = true;
                HotkeyPressed?.Invoke(this, new HotkeyEventArgs { ForegroundWindow = Win32Interop.GetForegroundWindow() });
            }
            if (isKeyUp && _hotkeyActive && IsModifierKey(vkCode) && ModifierMissing())
            {
                _hotkeyActive = false;
                HotkeyReleased?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            // Release modifier while hotkey active
            if (isKeyUp && _hotkeyActive && ModifierMissing())
            {
                _hotkeyActive = false;
                HotkeyReleased?.Invoke(this, EventArgs.Empty);
            }

            if (vkCode == _hotkeyVk)
            {
                var allMods = ModifiersMatch();
                var anyMod = _requireCtrl || _requireShift || _requireAlt || _requireWin;

                if (isKeyDown && allMods && anyMod && !_hotkeyActive)
                {
                    _hotkeyActive = true;
                    HotkeyPressed?.Invoke(this, new HotkeyEventArgs { ForegroundWindow = Win32Interop.GetForegroundWindow() });
                    if (_requireWin) return (IntPtr)1;
                }
                else if (isKeyUp && _hotkeyActive)
                {
                    _hotkeyActive = false;
                    HotkeyReleased?.Invoke(this, EventArgs.Empty);
                    if (_requireWin) return (IntPtr)1;
                }
                else if (_winPressed && _requireWin)
                {
                    return (IntPtr)1;
                }
            }
        }

        return Win32Interop.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private bool ModifiersMatch() =>
        (!_requireCtrl || _ctrlPressed) &&
        (!_requireShift || _shiftPressed) &&
        (!_requireAlt || _altPressed) &&
        (!_requireWin || _winPressed);

    private bool ModifierMissing() =>
        (_requireCtrl && !_ctrlPressed) ||
        (_requireShift && !_shiftPressed) ||
        (_requireAlt && !_altPressed) ||
        (_requireWin && !_winPressed);

    private string BuildDisplayString(int vkCode)
    {
        var parts = new List<string>();
        if (_recordCtrl) parts.Add("Ctrl");
        if (_recordShift) parts.Add("Shift");
        if (_recordAlt) parts.Add("Alt");
        if (_recordWin) parts.Add("Win");
        if (vkCode != 0) parts.Add(GetKeyName(vkCode));
        return string.Join("+", parts);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            Win32Interop.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }
}
