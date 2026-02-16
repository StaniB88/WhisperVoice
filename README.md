# Whisper Voice

Local speech-to-text for Windows using OpenAI Whisper — like Wispr Flow, but free and fully offline.

![Windows](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![.NET](https://img.shields.io/badge/.NET-10-purple)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Push-to-Talk & Toggle Mode** — customizable global hotkey (default: `Win+D`)
- **100% Local** — no cloud, no API keys, complete privacy
- **Single Process** — one .NET executable, no Python, no FFmpeg, no Node.js
- **GPU Acceleration** — CUDA support for NVIDIA GPUs with automatic CPU fallback
- **Auto-Paste** — transcribed text is copied to clipboard and pasted at cursor
- **Multiple Languages** — German, English, and many more, or auto-detect
- **System Tray** — runs quietly in the background
- **Floating Status Bar** — always-visible recording indicator
- **Auto-Updates** — delta updates via Velopack

## Requirements

- Windows 10/11 (64-bit)
- NVIDIA GPU with CUDA (optional, recommended for faster transcription)

## Installation

Download the latest installer from [Releases](https://github.com/StaniB88/WhisperVoice/releases).

The installer handles everything — no dependencies to install manually.

### Build from Source

```bash
git clone https://github.com/StaniB88/WhisperVoice.git
cd WhisperVoice
dotnet run --project src/WhisperVoice
```

#### Publish (self-contained)

```bash
dotnet publish src/WhisperVoice -c Release -r win-x64 --self-contained -o publish
```

## Usage

1. **Launch** — the app opens with a setup wizard on first run
2. **Hold Hotkey** — press and hold your configured hotkey (default: `Win+D`)
3. **Speak** — your audio is recorded locally
4. **Release** — audio is transcribed using Whisper
5. **Done** — text is pasted at your cursor position

## Configuration

Settings are stored in `%APPDATA%/whisper-voice/config.json`

| Option | Description |
|--------|-------------|
| **Whisper Model** | tiny (fast) → large (best quality) |
| **Language** | German, English, auto-detect, and more |
| **Hotkey** | Any key combo, recorded in settings |
| **Toggle Mode** | Press once to start, press again to stop |
| **Auto-Paste** | Automatically paste with Ctrl+V after transcription |
| **Floating Bar** | Show/hide the floating status indicator |
| **Theme** | Multiple dark themes to choose from |

## Whisper Models

Models are downloaded on first use and stored in `%APPDATA%/whisper-voice/models/`.

| Model | Size | VRAM | Speed | Quality |
|-------|------|------|-------|---------|
| Tiny | ~75 MB | ~1 GB | ~10x | Basic |
| Base | ~142 MB | ~1 GB | ~7x | Good |
| Small | ~466 MB | ~2 GB | ~4x | Better |
| Medium | ~1.5 GB | ~5 GB | ~2x | Great |
| Large v3 | ~2.9 GB | ~10 GB | 1x | Best |
| Large Turbo | ~1.5 GB | ~6 GB | ~8x | Best |

English-only variants (Tiny, Base, Small, Medium) are also available for improved accuracy on English speech.

## Architecture

Single-process .NET 10 application — no child processes, no IPC, no HTTP servers.

```
User presses hotkey
    │
    ▼
Win32 SetWindowsHookEx (global keyboard hook)
    │
    ▼
NAudio (16kHz/16-bit/mono WAV capture)
    │
    ▼
Whisper.net (CUDA → CPU fallback)
    │
    ▼
Clipboard + SendInput (auto-paste at cursor)
```

| Component | Technology |
|-----------|-----------|
| UI | Avalonia UI 11 |
| MVVM | CommunityToolkit.Mvvm |
| Transcription | Whisper.net (whisper.cpp bindings) |
| Audio | NAudio |
| Hotkeys | Win32 P/Invoke |
| Updates | Velopack |

## Troubleshooting

### Slow transcription
- Use a smaller model (Tiny or Base)
- Install NVIDIA CUDA drivers for GPU acceleration
- Ensure GPU drivers are up to date

### Hotkey not working
- Some key combinations are reserved by Windows
- Try a different hotkey in Settings → Hotkey → Record

### Model download fails
- Check your internet connection
- Models are downloaded from Hugging Face — ensure it's not blocked

## License

MIT License — see [LICENSE](LICENSE) for details.

## Credits

- [Whisper.net](https://github.com/sandrohanea/whisper.net) — C# bindings for whisper.cpp
- [Avalonia UI](https://avaloniaui.net/) — cross-platform UI framework
- [NAudio](https://github.com/naudio/NAudio) — .NET audio library
- [Velopack](https://velopack.io/) — installer and auto-update framework
- [OpenAI Whisper](https://github.com/openai/whisper) — speech recognition model

## Support

If you find Whisper Voice useful, consider [buying me a coffee](https://buymeacoffee.com/anyautomation).
