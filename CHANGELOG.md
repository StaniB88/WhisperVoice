# Changelog

All notable changes to Whisper Voice are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.0.0] — 2026-05-22

### Added
- **Quantized Whisper models** — Large (Q5_0) ~1.1 GB and Large Turbo (Q5_0) ~574 MB
  for noticeably smaller download size and VRAM footprint with near-Best quality.
- **Selectable transcription text** — Last Transcription on Home, all History items and
  Notes items are now selectable; text can be marked with the mouse and copied via Ctrl+C.
- **Maximize button** in the title bar; native Windows 11 Snap-Layouts hover menu is wired
  via `Win32Properties.NonClientHitTestResult` on Minimize/Maximize/Close buttons.
- `THIRD-PARTY-NOTICES.md` listing every runtime/build dependency with its upstream license.
- `COPYING` (GPL-3.0) and `COPYING.LESSER` (LGPL-3.0) reference files.

### Changed
- **License changed from MIT to LGPL-3.0-or-later.** Every `.cs` file now carries the
  standard LGPL header crediting AnyAutomation.
- **Upgraded Avalonia 11.3.12 → 12.0.3** (major). `Avalonia.Diagnostics` was replaced by
  `AvaloniaUI.DiagnosticsSupport` 2.2.1.
- Window decorations migrated to the Avalonia 12 model — `SystemDecorations="None"` /
  `ExtendClientAreaChromeHints="NoChrome"` were replaced by `WindowDecorations="BorderOnly"`
  on the main/setup windows and `WindowDecorations="None"` on the floating bar/donate dialog.
- `Watermark` → `PlaceholderText` on notes editor and search box (Avalonia 12 rename).
- Updated runtime dependencies: CommunityToolkit.Mvvm 8.4.2, Microsoft.Extensions.DependencyInjection 10.0.8,
  Serilog 4.3.1, Serilog.Sinks.Console 6.1.1, Serilog.Sinks.File 7.0.0, NAudio 2.3.0.
- Updated test dependencies: coverlet.collector 10.0.1, FluentAssertions 8.10.0,
  Microsoft.NET.Test.Sdk 18.5.1, xunit.runner.visualstudio 3.1.5.
- README badge, license section, model table and architecture box reflect the above.

### Fixed
- **Notes — Save no longer crashes the app.** Removing `x:CompileBindings="False"` from the
  Notes page restores compile-time type resolution for the `vm:NotesViewModel` reference inside
  the ListBox `DataTemplate`. The old reflection path threw
  `System.ArgumentException: Unable to resolve type vm:NotesViewModel`.
- **Window can be resized again.** The Avalonia 12 migration initially used
  `WindowDecorations="None"`, which removed the resize border; restored via `BorderOnly`.
- Removed the obsolete `DisableAvaloniaDataAnnotationValidation` workaround — `BindingPlugins`
  no longer exists in Avalonia 12 (data annotation validation is opt-in only there).

### Removed
- The Avalonia 11-era `Avalonia.Diagnostics` package reference.

---

## [2.0.4] — 2026-02-18

### Fixed
- Update notification not showing on startup.
- Suppressed Visual Studio XAML design-time errors; main window height increased.

## [2.0.3] — earlier

Maintenance release.

## [2.0.2] — earlier

Maintenance release.

## [2.0.0] — earlier

Complete rewrite on Avalonia UI. Single-process .NET application replacing the prior
Python/FFmpeg-based stack.

## [1.x] — historical

Initial Python-based releases. See `git log` for details.
