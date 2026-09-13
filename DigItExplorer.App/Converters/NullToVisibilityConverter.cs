using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DigItExplorer.App.Converters;

/// <summary>Converts a present value to Visibility with optional inverted evaluation.</summary>
internal sealed class NullToVisibilityConverter : IValueConverter
{
    /// <summary>Converts a null or empty value to Collapsed, optionally inverted.</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool present = value is string text ? text.Length > 0 : value is not null;
        if (string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase)) present = !present;
        return present ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Not supported for one-way binding.</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
