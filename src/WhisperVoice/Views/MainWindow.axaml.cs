using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using WhisperVoice.ViewModels;

namespace WhisperVoice.Views;

public partial class MainWindow : Window
{
    private const string ActiveClass = "active";
    private SettingsViewModel? _settingsVm;
    private NotesViewModel? _notesVm;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // Allow dragging the window from the titlebar area
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var position = e.GetPosition(TitleBar);
            if (position.Y >= 0 && position.Y <= TitleBar.Bounds.Height &&
                position.X >= 0 && position.X <= TitleBar.Bounds.Width)
            {
                BeginMoveDrag(e);
            }
        }
    }

    private void MinimizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseClick(object? sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void NavHomeClick(object? sender, RoutedEventArgs e)
    {
        ShowPage("Home");
    }

    private void NavSettingsClick(object? sender, RoutedEventArgs e)
    {
        _settingsVm ??= App.Services.GetRequiredService<SettingsViewModel>();
        SettingsPage.DataContext = _settingsVm;
        ShowPage("Settings");
    }

    private void NavHistoryClick(object? sender, RoutedEventArgs e)
    {
        ShowPage("History");
    }

    private void NavNotesClick(object? sender, RoutedEventArgs e)
    {
        _notesVm ??= App.Services.GetRequiredService<NotesViewModel>();
        NotesPage.DataContext = _notesVm;
        ShowPage("Notes");
    }

    private void ShowPage(string page)
    {
        // Toggle page visibility
        HomePage.IsVisible = page == "Home";
        SettingsPage.IsVisible = page == "Settings";
        HistoryPage.IsVisible = page == "History";
        NotesPage.IsVisible = page == "Notes";

        // Update active sidebar button styles
        SetNavActive(NavHome, page == "Home");
        SetNavActive(NavSettings, page == "Settings");
        SetNavActive(NavHistory, page == "History");
        SetNavActive(NavNotes, page == "Notes");
    }

    private void DonateCardClick(object? sender, PointerPressedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = AppConstants.DonateUrl,
            UseShellExecute = true
        });
    }

    private static void SetNavActive(Button button, bool isActive)
    {
        if (isActive)
        {
            if (!button.Classes.Contains(ActiveClass))
                button.Classes.Add(ActiveClass);
        }
        else
        {
            button.Classes.Remove(ActiveClass);
        }
    }
}
