using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Whisper.net;
using Whisper.net.Ggml;
using Whisper.net.LibraryLoader;
using WhisperVoice.Models;

namespace WhisperVoice.Services;

public sealed class WhisperNetService : ITranscriptionService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private WhisperFactory? _factory;

    public bool IsModelLoaded => _factory is not null;
    public string? ActiveDevice { get; private set; }

    public async Task LoadModelAsync(string modelPath, CancellationToken ct = default)
    {
        Log.Debug("LoadModelAsync waiting for gate");
        await _gate.WaitAsync(ct);
        try
        {
            DisposeCore();
            Log.Information("Loading model from {Path} on background thread", modelPath);

            var (factory, device) = await Task.Run(() =>
            {
                Log.Debug("Setting runtime library order: CUDA first, CPU fallback");
                RuntimeOptions.RuntimeLibraryOrder =
                [
                    RuntimeLibrary.Cuda,
                    RuntimeLibrary.Cpu
                ];

                Log.Debug("Creating WhisperFactory from {Path}", modelPath);
                var f = WhisperFactory.FromPath(modelPath);

                // Build a throwaway processor to force runtime selection
                Log.Debug("Creating probe processor to detect available runtime...");
                using var probe = f.CreateBuilder().WithLanguage(AppConstants.AutoDetectLanguage).Build();

                var loaded = RuntimeOptions.LoadedLibrary;
                var d = loaded switch
                {
                    RuntimeLibrary.Cuda => "GPU (CUDA)",
                    _ => "CPU"
                };

                if (loaded != RuntimeLibrary.Cuda)
                {
                    Log.Warning(
                        "CUDA runtime was not loaded — falling back to CPU. " +
                        "Possible causes: (1) NVIDIA GPU drivers not installed or outdated, " +
                        "(2) GPU does not support CUDA, " +
                        "(3) Insufficient VRAM for the selected model, " +
                        "(4) CUDA toolkit libraries missing");
                }
                else
                {
                    Log.Information("CUDA runtime loaded successfully");
                }

                return (f, d);
            }, ct);

            _factory = factory;
            ActiveDevice = device;
            Log.Information("Model loaded, runtime: {Device}", device);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string language = AppConstants.AutoDetectLanguage, CancellationToken ct = default)
    {
        Log.Debug("TranscribeAsync waiting for gate");
        await _gate.WaitAsync(ct);
        try
        {
            if (_factory is null)
                throw new InvalidOperationException(Strings.ExModelNotLoaded);

            var factory = _factory;
            Log.Information("Starting transcription (language={Language})", language);

            var result = await Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();

                // Create processor with the requested language
                using var processor = factory.CreateBuilder()
                    .WithLanguage(language)
                    .Build();

                var segments = new List<string>();
                string? detectedLanguage = null;

                await foreach (var segment in processor.ProcessAsync(audioStream, ct))
                {
                    segments.Add(segment.Text);
                    detectedLanguage ??= segment.Language;
                }

                sw.Stop();
                var text = string.Join(" ", segments).Trim();
                return new TranscriptionResult(text, sw.Elapsed, detectedLanguage);
            }, ct);

            Log.Information("Transcription completed in {Duration:F1}s, {WordCount} words, lang={Language}",
                result.Duration.TotalSeconds, result.Text.Split(' ').Length, result.DetectedLanguage);
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    private void DisposeCore()
    {
        _factory?.Dispose();
        _factory = null;
    }

    public void Dispose()
    {
        _gate.Wait();
        try
        {
            DisposeCore();
        }
        finally
        {
            _gate.Release();
        }
        _gate.Dispose();
    }
}
