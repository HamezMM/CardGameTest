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

    /// <summary>Generic once-per-round latch for boss abilities that fire a single time each round
    /// (Bogfather's heal, Magmapincer's burn, Tideshell's split). Safe to share across bosses since
    /// only one boss is in play per round. Reset in <see cref="Gamma931.Core.Game.RoundEngine.RevealNextLocation"/>.</summary>
    public bool BossAbilityUsedThisRound { get; set; }

    /// <summary>Generic once-per-round counter for boss abilities capped at N triggers per round
    /// (Vinewarden's regen). Reset alongside <see cref="BossAbilityUsedThisRound"/>.</summary>
    public int BossAbilityTicksThisRound { get; set; }

    /// <summary>Players still owed an equipment turn this Player Turns step.</summary>
    public Queue<Player> PendingEquipmentTurns { get; } = new();

    /// <summary>Crab attackers ("Boss" / "Minion") still owed an attack this wave (Boss Attack
    /// step queues at most one "Boss" entry; Minion Attacks step queues one "Minion" entry per
    /// living minion).</summary>
    public Queue<string> PendingCrabAttacks { get; } = new();

    /// <summary>Cached result of <see cref="Gamma931.Core.Game.RoundEngine.PeekNextCrabAttackTarget"/>
    /// for the attacker at the front of <see cref="PendingCrabAttacks"/>, so the UI can preview which
    /// player's protection cards are blockable without re-rolling Wreckstalker's ambush chance a
    /// second time when the attack is actually resolved.</summary>
    public Player? PendingCrabAttackTarget { get; set; }

    public List<GameEvent> Log { get; } = new();

    public int AliveMinionCount => CurrentMinions.Count;
    public bool AnyCrabsAlive => CurrentBossHp > 0 || AliveMinionCount > 0;
    public bool AnyPlayersAlive => Players.Any(p => p.IsAlive);

    public void LogEvent(string message) => Log.Add(new GameEvent(message));
}
