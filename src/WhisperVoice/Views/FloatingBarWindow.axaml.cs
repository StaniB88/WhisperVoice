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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace WhisperVoice.Views;

public partial class FloatingBarWindow : Window
{
    public FloatingBarWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        var screen = Screens.Primary;
        if (screen is null) return;

        var workArea = screen.WorkingArea;
        var scaling = screen.Scaling;
        var barWidth = Width > 0 ? Width : 250;
        var barHeight = Height > 0 ? Height : 40;

        Position = new PixelPoint(
            (int)(workArea.X + (workArea.Width - barWidth * scaling) / 2),
            (int)(workArea.Y + workArea.Height - (barHeight + 20) * scaling));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void ShowMainWindowClick(object? sender, RoutedEventArgs e)
    {
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is not null)
        {
            desktop.MainWindow.Show();
            desktop.MainWindow.WindowState = WindowState.Normal;
            desktop.MainWindow.Activate();
        }
    }

    private void HideFloatingBarClick(object? sender, RoutedEventArgs e)
    {
        if (App.Current is App app)
            app.HideFloatingBar();
    }

    private void QuitClick(object? sender, RoutedEventArgs e)
    {
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}
