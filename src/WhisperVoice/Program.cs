// Copyright (C) 2026 AnyAutomation
//
// This file is part of WhisperVoice.
//
// WhisperVoice is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// WhisperVoice is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with WhisperVoice.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Threading;
using Avalonia;
using Serilog;
using Velopack;

namespace WhisperVoice;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack MUST be first -- handles install/update/uninstall hooks
        VelopackApp.Build()
            .OnFirstRun(v =>
            {
                // Runs once after first install
                // Desktop shortcut is created by Velopack automatically
            })
            .Run();

        // Single instance lock
        using var mutex = new Mutex(true, AppConstants.MutexName, out bool isNew);
        if (!isNew) return;

        // Set up Serilog
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
            .WriteTo.Console()
#else
            .MinimumLevel.Information()
#endif
            .WriteTo.File(AppConstants.LogFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: AppConstants.LogRetainedFileDays,
                outputTemplate: AppConstants.LogOutputTemplate)
            .CreateLogger();

        try
        {
            Log.Information("WhisperVoice starting");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.Information("WhisperVoice shutting down");
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
