using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DigItExplorer.App.Converters;

/// <summary>Converts boolean values to Visibility with optional inverted evaluation.</summary>
internal sealed class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>Converts a boolean value to Visibility, optionally inverted.</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is bool v && v;
        if (string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase)) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Not supported for one-way binding.</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
