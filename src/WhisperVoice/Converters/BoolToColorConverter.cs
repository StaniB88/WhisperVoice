using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WhisperVoice.Converters;

/// <summary>
/// Converts a boolean to one of two brushes.
/// ConverterParameter format: "TrueValue|FalseValue"
/// Values can be hex colors (e.g., "#e94560") or resource keys (e.g., "Highlight").
/// </summary>
public sealed class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string param)
            return Brushes.Transparent;

        var parts = param.Split('|');
        if (parts.Length != 2)
            return Brushes.Transparent;

        var token = boolValue ? parts[0].Trim() : parts[1].Trim();
        return ResolveBrush(token);
    }

    private static IBrush ResolveBrush(string token)
    {
        if (string.Equals(token, "Transparent", StringComparison.OrdinalIgnoreCase))
            return Brushes.Transparent;

        // Try resource key first
        if (!token.StartsWith('#') && Application.Current is not null &&
            Application.Current.TryFindResource(token, Application.Current.ActualThemeVariant, out var resource))
        {
            if (resource is IBrush brush)
                return brush;
            if (resource is Color color)
                return new SolidColorBrush(color);
        }

        // Fall back to hex parsing
        try
        {
            return SolidColorBrush.Parse(token);
        }
        catch
        {
            return Brushes.Transparent;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
