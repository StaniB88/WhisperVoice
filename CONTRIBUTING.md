# Contributing to Whisper Voice

Thanks for your interest in contributing!

## Getting Started

1. Fork the repository
2. Clone your fork
3. Install [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
4. Run the app: `dotnet run --project src/WhisperVoice`
5. Run tests: `dotnet test tests/WhisperVoice.Tests`

## Development

### Project Structure

```
src/WhisperVoice/
  Models/          Immutable data models (sealed records)
  Services/        Business logic behind interfaces
  ViewModels/      MVVM view models (CommunityToolkit.Mvvm)
  Views/           Avalonia AXAML views
  Platform/        Win32 P/Invoke interop
  Themes/          Theme resource dictionaries

tests/WhisperVoice.Tests/
  Models/          Model tests
  Services/        Service tests
```

### Architecture

- **MVVM** with CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`)
- **Dependency Injection** via `Microsoft.Extensions.DependencyInjection`
- **Interface-first** — every service has an interface for testability
- **Immutable models** — all models are `sealed record` types

### Coding Standards

- Follow .NET naming conventions (PascalCase public, _camelCase private)
- Keep methods under 50 lines
- Keep files under 800 lines
- Use `async/await` for I/O operations
- Handle errors with try/catch and meaningful messages
- No `Console.WriteLine` — use Serilog (`Log.Information`, `Log.Error`, etc.)

## Submitting Changes

1. Create a feature branch from `main`
2. Make your changes
3. Ensure `dotnet build` has 0 errors and 0 warnings
4. Ensure `dotnet test` passes all tests
5. Open a Pull Request with a clear description

## Reporting Issues

Use the [issue templates](https://github.com/StaniB88/WhisperVoice/issues/new/choose) to report bugs or request features.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
