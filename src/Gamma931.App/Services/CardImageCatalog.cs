using Gamma931.Core.Models;

namespace Gamma931.App.Services;

/// <summary>
/// Maps card models to their generated face art (see tools/generate_card_art.py), which is
/// built into the assembly as WPF Resources at Assets/Cards/&lt;category&gt;/&lt;id&gt;.png.
/// Damage cards share one image per <see cref="BodyLocation"/> instead of per-Id, since the
/// model itself only carries a body location (RULES.md: damage cards have no individual name).
/// </summary>
public static class CardImageCatalog
{
    private const string BasePath = "/Assets/Cards/";

    public static string Back => $"{BasePath}back.png";

    public static string For(CharacterCard card) => $"{BasePath}characters/{card.Id}.png";
    public static string For(CrabBossCard card) => $"{BasePath}bosses/{card.Id}.png";
    public static string For(CrabMinionCard card) => $"{BasePath}minions/{card.Id}.png";
    public static string For(EquipmentCard card) => $"{BasePath}equipment/{card.Id}.png";
    public static string For(LocationCard card) => $"{BasePath}locations/{card.Id}.png";
    public static string For(DamageCard card) => $"{BasePath}damage/{card.BodyLocation}.png";
}
