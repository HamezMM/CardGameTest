namespace Gamma931.Core.Models;

/// <summary>
/// A player-drawn equipment card. Playing a Melee or Ranged card sets the player's Position
/// (RULES.md: "melee/range position is determined by equipment played"). Protection cards block
/// a crab attack as an out-of-turn response; Healing cards must be played in normal turn order.
/// </summary>
public sealed class EquipmentCard
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required EquipmentType EquipmentType { get; init; }
    public required string EffectText { get; init; }
    public int DamageTier { get; init; }

    /// <summary>The stance playing this card puts the player in, or null if it doesn't move position.</summary>
    public Position? SetsPosition => EquipmentType switch
    {
        EquipmentType.Melee => Position.Melee,
        EquipmentType.Ranged => Position.Range,
        _ => null,
    };

    public override string ToString() => Name;
}
