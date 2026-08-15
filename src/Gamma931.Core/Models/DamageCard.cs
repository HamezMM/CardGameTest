namespace Gamma931.Core.Models;

/// <summary>
/// Drawn to resolve a crab attack. HpCost is fixed by RULES.md ("Damage & Health"): Arms 1,
/// Head 2, Legs 1, Torso 1. Deck composition (how many of each) is what CSV tuning controls.
/// </summary>
public sealed class DamageCard
{
    public required string Id { get; init; }
    public required BodyLocation BodyLocation { get; init; }

    public int HpCost => BodyLocation switch
    {
        BodyLocation.Arms => 1,
        BodyLocation.Head => 2,
        BodyLocation.Legs => 1,
        BodyLocation.Torso => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(BodyLocation)),
    };

    public override string ToString() => $"{BodyLocation} ({HpCost} HP)";
}
