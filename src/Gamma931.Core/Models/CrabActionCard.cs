namespace Gamma931.Core.Models;

/// <summary>
/// Drawn each combat-cycle step ("draw and resolve a crab action card" — RULES.md Combat Turn
/// Order). Exact effect text is still design-pending; EffectText is free-form until finalized.
/// </summary>
public sealed class CrabActionCard
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string EffectText { get; init; }

    public override string ToString() => Name;
}
