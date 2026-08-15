namespace Gamma931.Core.Models;

/// <summary>
/// A player-drawn equipment card. Every player has a default melee weapon and a default ranged
/// weapon, each dealing a flat 1 HP hit on its own (RULES.md "Weapons & Modifiers") — a Melee or
/// Ranged equipment card is not a stand-alone weapon but a weapon modifier (melee) or ammo
/// (ranged) that adds <see cref="DamageBonus"/> on top of that base 1 HP when played. Playing one
/// sets the player's Position (RULES.md: "melee/range position is determined by equipment
/// played"). Protection cards block a crab attack as an out-of-turn response; Healing cards must
/// be played in normal turn order.
/// </summary>
public sealed class EquipmentCard
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required EquipmentType EquipmentType { get; init; }
    public required string EffectText { get; init; }

    /// <summary>Bonus HP damage this modifier/ammo card adds on top of the default weapon's base
    /// 1 HP hit (Melee/Ranged cards only; meaningless for other equipment types).</summary>
    public int DamageBonus { get; init; }

    /// <summary>The stance playing this card puts the player in, or null if it doesn't move position.</summary>
    public Position? SetsPosition => EquipmentType switch
    {
        EquipmentType.Melee => Position.Melee,
        EquipmentType.Ranged => Position.Range,
        _ => null,
    };

    public override string ToString() => Name;
}
