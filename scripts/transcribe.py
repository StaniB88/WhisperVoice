#!/usr/bin/env python3
"""
Whisper Transkriptions-Script
Konvertiert Audio zu Text mit OpenAI Whisper
"""

import sys
import os
import warnings

# Warnungen unterdrücken
warnings.filterwarnings("ignore")
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'

# FFmpeg Pfad setzen (common installation locations)
FFMPEG_PATHS = [
    os.path.join(os.environ.get('APPDATA', ''), 'whisper-voice', 'dependencies', 'ffmpeg', 'ffmpeg-master-latest-win64-gpl', 'bin'),
    r"C:\ffmpeg\bin",
    r"C:\Program Files\ffmpeg\bin",
    r"C:\Program Files (x86)\ffmpeg\bin",
]

# FFmpeg zum PATH hinzufügen
for ffmpeg_path in FFMPEG_PATHS:
    if os.path.exists(ffmpeg_path):
        os.environ["PATH"] = ffmpeg_path + os.pathsep + os.environ.get("PATH", "")
        print(f"FFmpeg gefunden: {ffmpeg_path}", file=sys.stderr)
        break


def transcribe(audio_path: str, model_name: str = "base", language: str = "de") -> str:
    """
    Transkribiert eine Audio-Datei mit Whisper.

    Args:
        audio_path: Pfad zur Audio-Datei
        model_name: Whisper Modell (tiny, base, small, medium, large)
        language: Sprache (de, en, auto)

    Returns:
        Transkribierter Text
    """
    try:
        import whisper
    except ImportError:
        print("FEHLER: whisper nicht installiert. Bitte 'pip install openai-whisper' ausführen.", file=sys.stderr)
        sys.exit(1)

    # Modell laden (wird beim ersten Mal heruntergeladen)
    print(f"Lade Whisper Modell '{model_name}'...", file=sys.stderr)
    model = whisper.load_model(model_name)

    # Transkribieren
    print(f"Transkribiere '{audio_path}'...", file=sys.stderr)

    # GPU-Beschleunigung wenn verfügbar
    import torch
    use_gpu = torch.cuda.is_available()

    options = {
        "fp16": use_gpu,  # FP16 für GPU (viel schneller)
        "verbose": False
    }

    if use_gpu:
        print(f"GPU: {torch.cuda.get_device_name(0)}", file=sys.stderr)

    if language and language != "auto":
        options["language"] = language

    result = model.transcribe(audio_path, **options)

    text = result["text"].strip()
    print(f"Erkannt: {text}", file=sys.stderr)

    return text


def main():
    if len(sys.argv) < 2:
        print("Verwendung: python transcribe.py <audio_datei> [modell] [sprache]", file=sys.stderr)
        sys.exit(1)

    audio_path = sys.argv[1]
    model_name = sys.argv[2] if len(sys.argv) > 2 else "base"
    language = sys.argv[3] if len(sys.argv) > 3 else "de"

    if not os.path.exists(audio_path):
        print(f"FEHLER: Datei nicht gefunden: {audio_path}", file=sys.stderr)
        sys.exit(1)

    # Transkribieren
    text = transcribe(audio_path, model_name, language)

    # Ergebnis auf stdout (wird von Electron gelesen)
    print(text)


if __name__ == "__main__":
    main()
