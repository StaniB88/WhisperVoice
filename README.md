# Whisper Voice

Local speech-to-text for Windows using OpenAI Whisper - like Wispr Flow, but free and fully offline.

![Windows](https://img.shields.io/badge/platform-Windows-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![Electron](https://img.shields.io/badge/Electron-28-47848F)

## Features

- **Push-to-Talk** with customizable global hotkeys
- **100% Local** - No cloud, no API keys, complete privacy
- **Fast Transcription** - Whisper model stays loaded in memory
- **GPU Acceleration** - CUDA support for NVIDIA GPUs
- **Auto-Paste** - Automatically inserts transcribed text
- **Multiple Languages** - German, English, or auto-detect
- **System Tray** - Runs quietly in the background
- **Floating Status Bar** - Always visible recording indicator

## Requirements

- Windows 10/11 (64-bit)
- Python 3.10+
- Node.js 18+
- FFmpeg
- NVIDIA GPU with CUDA (optional, but recommended)

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/StaniB88/WhisperVoice.git
cd WhisperVoice
```

### 2. Install Python Dependencies

```bash
pip install openai-whisper

# For GPU acceleration (recommended):
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118
```

### 3. Install FFmpeg

```bash
# Using winget:
winget install ffmpeg

# Or using chocolatey:
choco install ffmpeg
```

### 4. Install Node.js Dependencies

```bash
npm install
```

### 5. Build the Helper Application (Optional)

The native Windows helper enables Win+key hotkey combinations:

```bash
cd helper
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained
```

Copy the output to `bin/WhisperHelper.exe`.

### 6. Start the App

```bash
npm start
```

## Usage

1. **Launch** - The app starts and minimizes to the system tray
2. **Hold Hotkey** - Press and hold your configured hotkey (default: `Alt+Space`)
3. **Speak** - Your audio is being recorded
4. **Release** - Audio is transcribed using Whisper
5. **Done** - Text is copied to clipboard and optionally auto-pasted

## Configuration

Settings are stored in `%APPDATA%/whisper-voice/config.json`

| Option | Description |
|--------|-------------|
| **Whisper Model** | tiny (fast) → large (best quality) |
| **Language** | German, English, or Auto-detect |
| **Auto-Paste** | Automatically paste with Ctrl+V |
| **Floating Bar** | Show/hide the floating status indicator |

## Whisper Models

| Model | VRAM | Speed | Quality |
|-------|------|-------|---------|
| tiny | ~1 GB | Very fast | Basic |
| base | ~1 GB | Fast | Good |
| small | ~2 GB | Medium | Very good |
| medium | ~5 GB | Slow | Excellent |
| large | ~10 GB | Very slow | Best |

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Electron App                          │
│  ┌─────────────────────────────────────────────────┐    │
│  │  Main Process (src/main.js)                      │    │
│  │  • Window management                             │    │
│  │  • Global hotkeys (uiohook-napi)                 │    │
│  │  • IPC communication                             │    │
│  └─────────────────────────────────────────────────┘    │
│                         │                                │
│  ┌─────────────────────────────────────────────────┐    │
│  │  Renderer (src/index.html, src/floatingBar.html) │    │
│  │  • Settings UI                                   │    │
│  │  • Audio recording (MediaRecorder API)           │    │
│  └─────────────────────────────────────────────────┘    │
└────────────────────────────┬────────────────────────────┘
                             │ HTTP :5555
┌────────────────────────────▼────────────────────────────┐
│              Python Whisper Server                       │
│              (scripts/whisper_server.py)                 │
│  • Keeps model loaded in memory                          │
│  • GPU acceleration with CUDA                            │
│  • POST /transcribe, GET /health, GET /preload           │
└─────────────────────────────────────────────────────────┘
```

## Build

```bash
# Windows Installer (NSIS)
npm run build

# With custom icon (recommended)
npm run build:icon

# Portable executable
npm run build:portable
```

Output will be in the `dist/` folder.

## Troubleshooting

### "whisper not found"
```bash
pip install openai-whisper
```

### "ffmpeg not found"
```bash
winget install ffmpeg
```

### Slow transcription
- Use a smaller model (tiny or base)
- Enable GPU acceleration with CUDA
- Ensure your GPU drivers are up to date

### Hotkey not working
- Some key combinations are reserved by Windows
- Try a different hotkey combination
- For Win+key combinations, the native helper is required

## License

MIT License - see [LICENSE](LICENSE) for details.

## Credits

- [OpenAI Whisper](https://github.com/openai/whisper) - Speech recognition model
- [Electron](https://electronjs.org/) - Desktop framework
- [uiohook-napi](https://github.com/SnosMe/uiohook-napi) - Global keyboard hooks
