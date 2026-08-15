using Gamma931.Core.Models;

namespace Gamma931.Core.Game;

/// <summary>Mutable state for one in-progress game, owned and advanced by <see cref="RoundEngine"/>.</summary>
public sealed class GameState
{
    public required List<Player> Players { get; init; }
    public required DifficultyLevel Difficulty { get; init; }

    public required Deck<EquipmentCard> EquipmentDeck { get; init; }
    public required Deck<DamageCard> DamageDeck { get; init; }
    public required Deck<CrabBossCard> BossDeck { get; init; }
    public required Deck<CrabActionCard> CrabActionDeck { get; init; }
    public required Deck<CrabMinionCard> CrabMinionDeck { get; init; }

    /// <summary>Locations still to be revealed this game, in reveal order. The shuttle is always last.</summary>
    public required List<LocationCard> RemainingLocations { get; init; }

    public GamePhase Phase { get; set; } = GamePhase.NotStarted;
    public int RoundNumber { get; set; }
    public int FirstPlayerIndex { get; set; }

    public LocationCard? CurrentLocation { get; set; }
    public CrabBossCard? CurrentBoss { get; set; }
    public int CurrentBossHp { get; set; }
    public List<CrabMinionCard> CurrentMinions { get; } = new();

    /// <summary>Face-down pile set aside for the round (RULES.md step 4), drawn down through combat.</summary>
    public Queue<CrabActionCard> CrabActionsThisRound { get; } = new();
    public CrabActionCard? CurrentCrabAction { get; set; }

    /// <summary>Set by DrawCrabAction when a crab action card triggers an attack still awaiting
    /// resolution (so the target gets a chance to block before equipment turns continue).</summary>
    public Player? PendingCrabActionTarget { get; set; }

    /// <summary>Players still owed an equipment play this combat cycle / End Phase pass.</summary>
    public Queue<Player> PendingEquipmentTurns { get; } = new();

    /// <summary>Crab attackers ("Boss" / "Minion") still owed an attack this wave of End Phase Combat.</summary>
    public Queue<string> PendingCrabAttacks { get; } = new();

    public List<GameEvent> Log { get; } = new();

    public int AliveMinionCount => CurrentMinions.Count;
    public bool AnyCrabsAlive => CurrentBossHp > 0 || AliveMinionCount > 0;
    public bool AnyPlayersAlive => Players.Any(p => p.IsAlive);

    public void LogEvent(string message) => Log.Add(new GameEvent(message));
}
