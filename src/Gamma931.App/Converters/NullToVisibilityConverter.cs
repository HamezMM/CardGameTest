using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Gamma931.App.Converters;

/// <summary>Collapses a bound element when its source value is null (e.g. no boss/location revealed yet).</summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
