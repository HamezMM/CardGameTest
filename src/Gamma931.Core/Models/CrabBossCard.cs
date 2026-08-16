using System.Linq;

namespace Gamma931.Core.Models;

/// <summary>
/// A boss crab. Biome is empty for universal bosses. HpByPlayerCount and MinionCountByPlayerCount
/// are placeholder starting values (RULES.md never pins down real numbers) — tune them via the CSV
/// during playtesting. RULES.md: "Difficulty scales instead via boss minion count scaling with
/// player count" — both the boss's HP pool and the minion escort it brings are printed on the
/// card per player count, the same pattern LocationCard uses for its own numbers.
/// </summary>
public sealed class CrabBossCard
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required BossCategory Category { get; init; }
    public string Biome { get; init; } = string.Empty;
    public required string Concept { get; init; }
    public string AbilityText { get; init; } = string.Empty;
    public string FlavorText { get; init; } = string.Empty;

    public required IReadOnlyDictionary<int, int> HpByPlayerCount { get; init; }
    public required IReadOnlyDictionary<int, int> MinionCountByPlayerCount { get; init; }

    public int HpFor(int playerCount) => Lookup(HpByPlayerCount, playerCount);
    public int MinionCountFor(int playerCount) => Lookup(MinionCountByPlayerCount, playerCount);

    /// <summary>Printed-card summary of HP across 2-5 players, e.g. "6/7/8/9 HP".</summary>
    public string HpSummary => FormatSummary(HpByPlayerCount, "HP");

    /// <summary>Printed-card summary of minion escort size across 2-5 players, e.g. "1/4/7/8 minions".</summary>
    public string MinionSummary => FormatSummary(MinionCountByPlayerCount, "minions");

    /// <summary>Combined footer line for the printed card face: HP and minion escort, both by player count (2-5).</summary>
    public string StatsSummary => $"{HpSummary} · {MinionSummary}";

    public bool IsActiveAt(string locationBiome) =>
        Category == BossCategory.Universal || string.Equals(Biome, locationBiome, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Name;

    private static string FormatSummary(IReadOnlyDictionary<int, int> table, string unit) =>
        $"{string.Join("/", new[] { 2, 3, 4, 5 }.Select(p => Lookup(table, p)))} {unit}";

    private static int Lookup(IReadOnlyDictionary<int, int> table, int playerCount)
    {
        if (!table.TryGetValue(playerCount, out var value))
        {
            throw new ArgumentOutOfRangeException(nameof(playerCount), playerCount,
                $"No value configured for {playerCount} players (expected 2-5).");
        }

        return value;
    }
}
