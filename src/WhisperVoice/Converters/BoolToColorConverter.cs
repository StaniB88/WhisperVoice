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
