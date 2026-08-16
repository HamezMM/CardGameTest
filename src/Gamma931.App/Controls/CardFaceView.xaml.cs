using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Gamma931.App.Controls;

/// <summary>
/// Generic "UI engine" card widget: art on top (looked up from whatever object is bound to
/// <see cref="Card"/> via the CardImage converter), name/flavor/effect text underneath. Every
/// card-shaped surface in the app -- the browser gallery, a player's hand, the minion row --
/// builds on this one control instead of hand-rolling its own card layout.
/// </summary>
public partial class CardFaceView : UserControl
{
    public static readonly DependencyProperty CardProperty = DependencyProperty.Register(
        nameof(Card), typeof(object), typeof(CardFaceView));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(CardFaceView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle), typeof(string), typeof(CardFaceView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty BodyTextProperty = DependencyProperty.Register(
        nameof(BodyText), typeof(string), typeof(CardFaceView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty FlavorTextProperty = DependencyProperty.Register(
        nameof(FlavorText), typeof(string), typeof(CardFaceView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty FooterTextProperty = DependencyProperty.Register(
        nameof(FooterText), typeof(string), typeof(CardFaceView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
        nameof(AccentBrush), typeof(Brush), typeof(CardFaceView), new PropertyMetadata(Brushes.SlateGray));

    /// <summary>The card model (CharacterCard, EquipmentCard, ...) whose art should be shown.</summary>
    public object? Card
    {
        get => GetValue(CardProperty);
        set => SetValue(CardProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string BodyText
    {
        get => (string)GetValue(BodyTextProperty);
        set => SetValue(BodyTextProperty, value);
    }

    /// <summary>Italicized flavor/lore text, rendered above the rules text (<see cref="BodyText"/>) — the same
    /// "flavor above rules" convention most trading card games use to keep mechanics and story visually distinct.</summary>
    public string FlavorText
    {
        get => (string)GetValue(FlavorTextProperty);
        set => SetValue(FlavorTextProperty, value);
    }

    public string FooterText
    {
        get => (string)GetValue(FooterTextProperty);
        set => SetValue(FooterTextProperty, value);
    }

    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public CardFaceView()
    {
        InitializeComponent();
    }
}

/// <summary>Collapses a bound TextBlock when its source string is null/empty/whitespace.</summary>
public sealed class EmptyToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
