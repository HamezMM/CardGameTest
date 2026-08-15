namespace Gamma931.Core.Models;

/// <summary>
/// A boss crab. Biome is empty for universal bosses. StartingHp is a placeholder starting value
/// (RULES.md never pins down real boss HP numbers) — tune it via the CSV during playtesting.
/// </summary>
public sealed class CrabBossCard
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required BossCategory Category { get; init; }
    public string Biome { get; init; } = string.Empty;
    public required string Concept { get; init; }
    public string AbilityText { get; init; } = string.Empty;
    public required int StartingHp { get; init; }

    public bool IsActiveAt(string locationBiome) =>
        Category == BossCategory.Universal || string.Equals(Biome, locationBiome, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Name;
}
