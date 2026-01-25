#!/usr/bin/env python3
"""
Whisper Server - Hält das Modell im Speicher für schnelle Transkription
"""

import sys
import os
import json
import warnings
from http.server import HTTPServer, BaseHTTPRequestHandler
import threading

# Warnungen unterdrücken
warnings.filterwarnings("ignore")
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'

# FFmpeg Pfad setzen
# Zuerst Umgebungsvariable prüfen (vom Setup), dann Fallback-Pfade
FFMPEG_PATHS = [
    os.environ.get("FFMPEG_PATH", ""),  # Vom Setup gesetzt
    r"C:\ffmpeg\bin",
    r"C:\Program Files\ffmpeg\bin",
    r"C:\Program Files (x86)\ffmpeg\bin",
]

ffmpeg_found = False
for ffmpeg_path in FFMPEG_PATHS:
    if ffmpeg_path and os.path.exists(ffmpeg_path):
        os.environ["PATH"] = ffmpeg_path + os.pathsep + os.environ.get("PATH", "")
        print(f"FFmpeg gefunden: {ffmpeg_path}", file=sys.stderr)
        ffmpeg_found = True
        break

if not ffmpeg_found:
    print("WARNUNG: FFmpeg nicht gefunden!", file=sys.stderr)

# Globales Modell (wird einmal geladen)
MODEL = None
MODEL_NAME = None

def load_model(model_name="base"):
    global MODEL, MODEL_NAME

    if MODEL is not None and MODEL_NAME == model_name:
        print(f"Modell '{model_name}' bereits geladen", file=sys.stderr)
        return MODEL

    import whisper
    import torch

    print(f"Lade Whisper Modell '{model_name}'...", file=sys.stderr)

    # GPU wenn verfügbar
    device = "cuda" if torch.cuda.is_available() else "cpu"

    if device == "cuda":
        gpu_name = torch.cuda.get_device_name(0)
        print(f"GPU erkannt: {gpu_name}", file=sys.stderr)

        # Load on GPU - no fallback, let errors propagate
        MODEL = whisper.load_model(model_name, device="cuda")
        MODEL_NAME = model_name
        print(f"Modell geladen auf CUDA ({gpu_name})", file=sys.stderr)
        return MODEL

    # Load on CPU only if CUDA not available
    MODEL = whisper.load_model(model_name, device="cpu")
    MODEL_NAME = model_name
    print(f"Modell geladen auf CPU", file=sys.stderr)
    return MODEL


def transcribe(audio_path, model_name="base", language="de"):
    global MODEL

    model = load_model(model_name)

    # Check actual device of the model (may have fallen back to CPU)
    model_device = str(next(model.parameters()).device)
    use_gpu = "cuda" in model_device

    options = {
        "fp16": use_gpu,  # fp16 only works on GPU
        "verbose": False
    }

    if language and language != "auto":
        options["language"] = language

    print(f"Transkribiere: {audio_path} (auf {model_device.upper()})", file=sys.stderr)
    result = model.transcribe(audio_path, **options)

    text = result["text"].strip()
    print(f"Erkannt: {text}", file=sys.stderr)

    return text


class WhisperHandler(BaseHTTPRequestHandler):
    def log_message(self, format, *args):
        # Logging unterdrücken
        pass

    def do_POST(self):
        if self.path == "/transcribe":
            content_length = int(self.headers['Content-Length'])
            post_data = self.rfile.read(content_length)

            try:
                data = json.loads(post_data.decode('utf-8'))
                audio_path = data.get('audio_path')
                model_name = data.get('model', 'base')
                language = data.get('language', 'de')

                text = transcribe(audio_path, model_name, language)

                self.send_response(200)
                self.send_header('Content-Type', 'application/json')
                self.end_headers()
                self.wfile.write(json.dumps({'text': text}).encode())

            except Exception as e:
                self.send_response(500)
                self.send_header('Content-Type', 'application/json')
                self.end_headers()
                self.wfile.write(json.dumps({'error': str(e)}).encode())
        else:
            self.send_response(404)
            self.end_headers()

    def do_GET(self):
        if self.path == "/health":
            # Get actual device if model is loaded
            device = None
            if MODEL is not None:
                device = str(next(MODEL.parameters()).device)

            self.send_response(200)
            self.send_header('Content-Type', 'application/json')
            self.end_headers()
            self.wfile.write(json.dumps({
                'status': 'ok',
                'model_loaded': MODEL is not None,
                'model_name': MODEL_NAME,
                'device': device
            }).encode())
        elif self.path == "/preload":
            # Modell vorladen
            model_name = self.headers.get('X-Model', 'base')
            load_model(model_name)
            self.send_response(200)
            self.send_header('Content-Type', 'application/json')
            self.end_headers()
            self.wfile.write(json.dumps({'status': 'loaded', 'model': model_name}).encode())
        else:
            self.send_response(404)
            self.end_headers()


def main():
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 5555
    model_name = sys.argv[2] if len(sys.argv) > 2 else "medium"

    # Modell beim Start laden
    print(f"Starte Whisper Server auf Port {port}...", file=sys.stderr)
    load_model(model_name)

    server = HTTPServer(('127.0.0.1', port), WhisperHandler)
    print(f"Server bereit: http://127.0.0.1:{port}", file=sys.stderr)
    print("Endpunkte:", file=sys.stderr)
    print("  POST /transcribe - Transkribieren", file=sys.stderr)
    print("  GET /health - Status", file=sys.stderr)
    print("  GET /preload - Modell vorladen", file=sys.stderr)

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nServer beendet", file=sys.stderr)
        server.shutdown()


if __name__ == "__main__":
    main()
