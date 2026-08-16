namespace Gamma931.Core.Models;

/// <summary>
/// A minion crab. Flat 1 HP by design (RULES.md: "Minion crabs are a flat 1 HP — any hit kills
/// one"). The card only exists for cosmetic/flavor variety, not stat variance.
/// </summary>
public sealed class CrabMinionCard
{
    public const int FixedHp = 1;

    public required string Id { get; init; }
    public required string Name { get; init; }
    public string FlavorText { get; init; } = string.Empty;

    public override string ToString() => Name;
}
