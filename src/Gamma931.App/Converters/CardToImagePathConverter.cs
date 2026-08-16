using System.Globalization;
using System.Windows.Data;
using Gamma931.App.Services;
using Gamma931.Core.Models;

namespace Gamma931.App.Converters;

/// <summary>
/// Binds any of the card model types straight to an <c>Image.Source</c> (WPF's built-in
/// string-&gt;ImageSource conversion handles the resulting pack-relative path). Centralizing the
/// type switch here means every card view -- browser gallery, hand, minion row -- can bind
/// <c>Source="{Binding Converter={StaticResource CardImage}}"</c> against the card object itself.
/// </summary>
public sealed class CardToImagePathConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        CharacterCard c => CardImageCatalog.For(c),
        CrabBossCard c => CardImageCatalog.For(c),
        CrabMinionCard c => CardImageCatalog.For(c),
        EquipmentCard c => CardImageCatalog.For(c),
        LocationCard c => CardImageCatalog.For(c),
        DamageCard c => CardImageCatalog.For(c),
        _ => CardImageCatalog.Back,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
