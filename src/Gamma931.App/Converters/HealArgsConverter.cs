using System.Globalization;
using System.Windows.Data;

namespace Gamma931.App.Converters;

/// <summary>
/// Packs a heal button's two independent binding sources — the Healing EquipmentCard being
/// played (the ancestor ItemsControl's DataContext) and the teammate to heal (the nested
/// ItemsControl's item) — into the single object[] parameter GameViewModel.HealTeammateCommand
/// expects, since a Button's CommandParameter only takes one bound value.
/// </summary>
public sealed class HealArgsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Clone();

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
