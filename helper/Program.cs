using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace WhisperHelper;

class Program
{
    // Win32 API Konstanten
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    // Virtual Key Codes
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;
    private const int VK_LCTRL = 0xA2;
    private const int VK_RCTRL = 0xA3;
    private const int VK_LSHIFT = 0xA0;
    private const int VK_RSHIFT = 0xA1;
    private const int VK_LALT = 0xA4;
    private const int VK_RALT = 0xA5;
    private const int VK_CTRL = 0x11;
    private const int VK_SHIFT = 0x10;
    private const int VK_ALT = 0x12;

    // Hook Handle
    private static IntPtr _hookID = IntPtr.Zero;
    private static LowLevelKeyboardProc _proc = HookCallback;

    // State
    private static bool _winPressed = false;
    private static bool _ctrlPressed = false;
    private static bool _shiftPressed = false;
    private static bool _altPressed = false;
    private static bool _hotkeyPressed = false;
    private static int _hotkeyVK = 0x44; // Default: D
    private static bool _requireWin = true;
    private static bool _requireCtrl = false;
    private static bool _requireShift = false;
    private static bool _requireAlt = false;

    // Recording Mode
    private static bool _recordingMode = false;
    // Track welche Modifier während Recording gedrückt wurden
    private static bool _recordCtrl = false;
    private static bool _recordShift = false;
    private static bool _recordAlt = false;
    private static bool _recordWin = false;

    private static TcpClient? _client;
    private static NetworkStream? _stream;
    private static readonly object _lock = new();

    // Port für Kommunikation mit Electron
    private const int PORT = 5556;

    // Win32 API Imports
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    static void Main(string[] args)
    {
        // Hotkey aus Argumenten lesen (optional)
        if (args.Length > 0 && int.TryParse(args[0], out int vk))
        {
            _hotkeyVK = vk;
        }

        Console.WriteLine($"WhisperHelper gestartet. Hotkey VK: {_hotkeyVK}");
        Console.WriteLine($"Verbinde zu Electron auf Port {PORT}...");

        // Verbindung zu Electron herstellen
        ConnectToElectron();

        // Hook installieren
        _hookID = SetHook(_proc);

        // Message Loop (nötig für Hook)
        System.Windows.Forms.Application.Run();

        // Cleanup
        UnhookWindowsHookEx(_hookID);
    }

    private static void ConnectToElectron()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    if (_client == null || !_client.Connected)
                    {
                        _client = new TcpClient();
                        await _client.ConnectAsync(IPAddress.Loopback, PORT);
                        _stream = _client.GetStream();
                        Console.WriteLine("Verbunden mit Electron!");
                        SendMessage("connected");

                        // Auf Nachrichten von Electron warten
                        _ = ListenForMessages();
                    }
                }
                catch
                {
                    // Retry nach 2 Sekunden
                    await Task.Delay(2000);
                }
            }
        });
    }

    private static async Task ListenForMessages()
    {
        var buffer = new byte[1024];
        try
        {
            while (_stream != null && _client?.Connected == true)
            {
                int bytesRead = await _stream.ReadAsync(buffer);
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    foreach (var line in message.Split('\n'))
                    {
                        ProcessMessage(line.Trim());
                    }
                }
            }
        }
        catch
        {
            // Verbindung verloren
            _client = null;
            _stream = null;
        }
    }

    private static void ProcessMessage(string message)
    {
        // Format: "setkey:ctrl,shift,alt,win,68" (modifiers + VK code)
        if (message.StartsWith("setkey:"))
        {
            var parts = message[7..].Split(',');
            if (parts.Length >= 5)
            {
                _requireCtrl = parts[0] == "1";
                _requireShift = parts[1] == "1";
                _requireAlt = parts[2] == "1";
                _requireWin = parts[3] == "1";
                if (int.TryParse(parts[4], out int vk))
                {
                    _hotkeyVK = vk;
                }
                var mods = new List<string>();
                if (_requireCtrl) mods.Add("Ctrl");
                if (_requireShift) mods.Add("Shift");
                if (_requireAlt) mods.Add("Alt");
                if (_requireWin) mods.Add("Win");
                mods.Add(((char)_hotkeyVK).ToString());
                Console.WriteLine($"Hotkey geändert zu: {string.Join("+", mods)}");
            }
        }
        else if (message == "record")
        {
            _recordingMode = true;
            // Reset recording state
            _recordCtrl = false;
            _recordShift = false;
            _recordAlt = false;
            _recordWin = false;
            Console.WriteLine("Recording-Modus aktiviert - warte auf Tastenkombination...");
        }
        else if (message == "cancelrecord")
        {
            _recordingMode = false;
            Console.WriteLine("Recording-Modus abgebrochen");
        }
        else if (message == "quit")
        {
            Environment.Exit(0);
        }
    }

    private static void SendMessage(string message)
    {
        lock (_lock)
        {
            try
            {
                if (_stream != null && _client?.Connected == true)
                {
                    byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                    _stream.Write(data, 0, data.Length);
                }
            }
            catch
            {
                // Ignorieren, Reconnect passiert automatisch
            }
        }
    }

    private static string GetKeyName(int vkCode)
    {
        // Buchstaben A-Z
        if (vkCode >= 0x41 && vkCode <= 0x5A)
            return ((char)vkCode).ToString();
        // Zahlen 0-9
        if (vkCode >= 0x30 && vkCode <= 0x39)
            return ((char)vkCode).ToString();
        // F-Tasten
        if (vkCode >= 0x70 && vkCode <= 0x7B)
            return $"F{vkCode - 0x6F}";
        // Space
        if (vkCode == 0x20)
            return "Space";
        return $"0x{vkCode:X2}";
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName!), 0);
    }

    private static bool IsModifierKey(int vkCode)
    {
        return vkCode == VK_LWIN || vkCode == VK_RWIN ||
               vkCode == VK_LCTRL || vkCode == VK_RCTRL || vkCode == VK_CTRL ||
               vkCode == VK_LSHIFT || vkCode == VK_RSHIFT || vkCode == VK_SHIFT ||
               vkCode == VK_LALT || vkCode == VK_RALT || vkCode == VK_ALT;
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            int vkCode = (int)hookStruct.vkCode;
            int msg = wParam.ToInt32();

            bool isKeyDown = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
            bool isKeyUp = msg == WM_KEYUP || msg == WM_SYSKEYUP;

            // Modifier Status tracken
            if (vkCode == VK_LWIN || vkCode == VK_RWIN)
            {
                _winPressed = isKeyDown;
                if (_recordingMode && isKeyDown) _recordWin = true;
            }
            if (vkCode == VK_LCTRL || vkCode == VK_RCTRL || vkCode == VK_CTRL)
            {
                _ctrlPressed = isKeyDown;
                if (_recordingMode && isKeyDown) _recordCtrl = true;
            }
            if (vkCode == VK_LSHIFT || vkCode == VK_RSHIFT || vkCode == VK_SHIFT)
            {
                _shiftPressed = isKeyDown;
                if (_recordingMode && isKeyDown) _recordShift = true;
            }
            if (vkCode == VK_LALT || vkCode == VK_RALT || vkCode == VK_ALT)
            {
                _altPressed = isKeyDown;
                if (_recordingMode && isKeyDown) _recordAlt = true;
            }

            // Recording Mode - fängt jede Tastenkombination ab
            if (_recordingMode)
            {
                // Option 1: Modifier + normale Taste
                if (isKeyDown && !IsModifierKey(vkCode))
                {
                    if (_recordCtrl || _recordShift || _recordAlt || _recordWin)
                    {
                        var mods = new List<string>();
                        if (_recordCtrl) mods.Add("Ctrl");
                        if (_recordShift) mods.Add("Shift");
                        if (_recordAlt) mods.Add("Alt");
                        if (_recordWin) mods.Add("Win");

                        string keyName = GetKeyName(vkCode);
                        mods.Add(keyName);

                        string combo = string.Join("+", mods);

                        // Format: recorded:ctrl,shift,alt,win,vkcode,displayname
                        string result = $"recorded:{(_recordCtrl ? "1" : "0")},{(_recordShift ? "1" : "0")},{(_recordAlt ? "1" : "0")},{(_recordWin ? "1" : "0")},{vkCode},{combo}";
                        SendMessage(result);
                        Console.WriteLine($"Aufgenommen: {combo}");

                        _recordingMode = false;

                        if (_recordWin) return (IntPtr)1;
                    }
                }

                // Option 2: Nur Modifier (z.B. Win+Ctrl)
                // Wenn mind. 2 Modifier gedrückt waren und alle losgelassen werden
                if (isKeyUp && IsModifierKey(vkCode))
                {
                    // Alle Modifier losgelassen?
                    bool allReleased = !_ctrlPressed && !_shiftPressed && !_altPressed && !_winPressed;

                    // Zähle wie viele Modifier während Recording gedrückt wurden
                    int recordedModCount = (_recordCtrl ? 1 : 0) + (_recordShift ? 1 : 0) +
                                          (_recordAlt ? 1 : 0) + (_recordWin ? 1 : 0);

                    if (allReleased && recordedModCount >= 2)
                    {
                        var mods = new List<string>();
                        if (_recordCtrl) mods.Add("Ctrl");
                        if (_recordShift) mods.Add("Shift");
                        if (_recordAlt) mods.Add("Alt");
                        if (_recordWin) mods.Add("Win");

                        string combo = string.Join("+", mods);

                        // VK code 0 = nur Modifier
                        string result = $"recorded:{(_recordCtrl ? "1" : "0")},{(_recordShift ? "1" : "0")},{(_recordAlt ? "1" : "0")},{(_recordWin ? "1" : "0")},0,{combo}";
                        SendMessage(result);
                        Console.WriteLine($"Aufgenommen (nur Modifier): {combo}");

                        _recordingMode = false;
                    }
                }
            }

            // Normal Hotkey Mode
            if (!_recordingMode)
            {
                bool isModifierOnly = _hotkeyVK == 0;

                if (isModifierOnly)
                {
                    // Modifier-only Hotkey (z.B. Ctrl+Win)
                    bool allModsPressed = (!_requireCtrl || _ctrlPressed) &&
                                         (!_requireShift || _shiftPressed) &&
                                         (!_requireAlt || _altPressed) &&
                                         (!_requireWin || _winPressed);

                    // Nur triggern wenn es ein neuer Keydown ist (keine Wiederholung)
                    if (isKeyDown && IsModifierKey(vkCode) && allModsPressed && !_hotkeyPressed)
                    {
                        _hotkeyPressed = true;
                        SendMessage("keydown");
                        Console.WriteLine("Hotkey gedrückt (Modifier-only)");
                    }

                    // Wenn einer der required Modifier losgelassen wird
                    if (isKeyUp && _hotkeyPressed && IsModifierKey(vkCode))
                    {
                        bool missingMod = (_requireCtrl && !_ctrlPressed) ||
                                         (_requireShift && !_shiftPressed) ||
                                         (_requireAlt && !_altPressed) ||
                                         (_requireWin && !_winPressed);
                        if (missingMod)
                        {
                            _hotkeyPressed = false;
                            SendMessage("keyup");
                            Console.WriteLine("Hotkey losgelassen (Modifier-only)");
                        }
                    }
                }
                else
                {
                    // Normal Hotkey mit Taste (z.B. Ctrl+Win+D)

                    // Wenn Modifier losgelassen und Hotkey noch gedrückt war
                    if (isKeyUp && _hotkeyPressed)
                    {
                        bool missingMod = (_requireCtrl && !_ctrlPressed) ||
                                         (_requireShift && !_shiftPressed) ||
                                         (_requireAlt && !_altPressed) ||
                                         (_requireWin && !_winPressed);
                        if (missingMod)
                        {
                            _hotkeyPressed = false;
                            SendMessage("keyup");
                            Console.WriteLine("Hotkey losgelassen (Modifier losgelassen)");
                        }
                    }

                    // Konfigurierte Taste prüfen
                    if (vkCode == _hotkeyVK)
                    {
                        bool allModsPressed = (!_requireCtrl || _ctrlPressed) &&
                                             (!_requireShift || _shiftPressed) &&
                                             (!_requireAlt || _altPressed) &&
                                             (!_requireWin || _winPressed);

                        bool anyModRequired = _requireCtrl || _requireShift || _requireAlt || _requireWin;

                        if (isKeyDown && allModsPressed && anyModRequired && !_hotkeyPressed)
                        {
                            _hotkeyPressed = true;
                            SendMessage("keydown");
                            Console.WriteLine("Hotkey gedrückt");
                            if (_requireWin) return (IntPtr)1;
                        }
                        else if (isKeyUp && _hotkeyPressed)
                        {
                            _hotkeyPressed = false;
                            SendMessage("keyup");
                            Console.WriteLine("Hotkey losgelassen");
                            if (_requireWin) return (IntPtr)1;
                        }
                        else if (_winPressed && _requireWin)
                        {
                            return (IntPtr)1;
                        }
                    }
                }
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }
}
