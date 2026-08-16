namespace Gamma931.Core.Models;

/// <summary>
/// A location card. One card serves every player count (RULES.md: "These numbers vary by player
/// count ... so one location deck serves all player counts"). The shuttle location has no combat
/// and ends the game when revealed.
/// </summary>
public sealed class LocationCard
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Biome { get; init; } = string.Empty;
    public bool IsShuttle { get; init; }
    public string FlavorText { get; init; } = string.Empty;

    public required IReadOnlyDictionary<int, int> EquipmentDrawByPlayerCount { get; init; }
    public required IReadOnlyDictionary<int, int> CrabActionCountByPlayerCount { get; init; }

    public int EquipmentDrawFor(int playerCount) => Lookup(EquipmentDrawByPlayerCount, playerCount);
    public int CrabActionCountFor(int playerCount) => Lookup(CrabActionCountByPlayerCount, playerCount);

    private static int Lookup(IReadOnlyDictionary<int, int> table, int playerCount)
    {
        if (!table.TryGetValue(playerCount, out var value))
        {
            throw new ArgumentOutOfRangeException(nameof(playerCount), playerCount,
                $"No value configured for {playerCount} players (expected 2-5).");
        }

        return value;
    }

    public override string ToString() => Name;
}
