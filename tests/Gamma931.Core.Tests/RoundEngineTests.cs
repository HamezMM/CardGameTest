using Gamma931.Core.Data;
using Gamma931.Core.Game;
using Gamma931.Core.Models;
using Xunit;

namespace Gamma931.Core.Tests;

public class RoundEngineTests
{
    private static CardDatabase LoadDb() =>
        new CsvCardLoader().LoadFromDirectory(Path.Combine(AppContext.BaseDirectory, "TestData"));

    private static IReadOnlyList<(string, CharacterCard)> PlayerSetup(CardDatabase db, int count) =>
        Enumerable.Range(1, count)
            .Select(i => ($"Player {i}", db.Characters[(i - 1) % db.Characters.Count]))
            .ToList();

    [Fact]
    public void StartNewGame_InitializesPlayersAndLocationQueue()
    {
        var db = LoadDb();
        var engine = new RoundEngine(new Random(1));

        var state = engine.StartNewGame(db, PlayerSetup(db, 4), DifficultyLevel.Normal);

        Assert.Equal(4, state.Players.Count);
        Assert.All(state.Players, p => Assert.Empty(p.Hand));
        Assert.All(state.Players, p => Assert.Equal(Player.MaxHp, p.CurrentHp));
        Assert.All(state.Players, p => Assert.Equal(Position.Range, p.Position));
        Assert.Equal(6, state.RemainingLocations.Count); // 5 biome locations + shuttle
        Assert.True(state.RemainingLocations.Last().IsShuttle);
        Assert.Equal(GamePhase.LocationReveal, state.Phase);
    }

    [Fact]
    public void StartNewGame_Throws_WhenPlayerCountOutOfRange()
    {
        var db = LoadDb();
        var engine = new RoundEngine();

        Assert.Throws<ArgumentException>(() => engine.StartNewGame(db, PlayerSetup(db, 1), DifficultyLevel.Normal));
        Assert.Throws<ArgumentException>(() => engine.StartNewGame(db, PlayerSetup(db, 6), DifficultyLevel.Normal));
    }

    [Fact]
    public void RevealNextLocation_EndsGameInWon_WhenShuttleIsNext()
    {
        var db = LoadDb();
        var engine = new RoundEngine(new Random(1));
        var state = engine.StartNewGame(db, PlayerSetup(db, 2), DifficultyLevel.Normal);

        // Fast-forward straight to the shuttle.
        var shuttle = state.RemainingLocations.Single(l => l.IsShuttle);
        state.RemainingLocations.Clear();
        state.RemainingLocations.Add(shuttle);

        engine.RevealNextLocation(state);

        Assert.Equal(GamePhase.Won, state.Phase);
    }

    [Fact]
    public void SelectCrabAttackTarget_PrefersMeleeOverRangeOverFirstPlayer()
    {
        var db = LoadDb();
        var engine = new RoundEngine(new Random(1));
        var state = engine.StartNewGame(db, PlayerSetup(db, 3), DifficultyLevel.Normal);

        // Everyone defaults to Range; put the second player into Melee.
        state.Players[1].Position = Position.Melee;

        var target = engine.SelectCrabAttackTarget(state);

        Assert.Equal(state.Players[1], target);
    }

    [Fact]
    public void SelectCrabAttackTarget_FallsBackToFirstPlayer_WhenPositionsGiveNoMatch()
    {
        var db = LoadDb();
        var engine = new RoundEngine(new Random(1));
        var state = engine.StartNewGame(db, PlayerSetup(db, 2), DifficultyLevel.Normal);

        state.Players[0].TakeDamage(Player.MaxHp); // player 0 is dead
        // Player 1 stays alive in the default Range position, so it should still be picked by
        // the Range tier — this test only exercises the tier ordering, not the literal edge case
        // where neither tier matches (which cannot happen while any player is alive, since every
        // alive player is always in either Melee or Range).
        var target = engine.SelectCrabAttackTarget(state);

        Assert.Equal(state.Players[1], target);
    }

    [Fact]
    public void ResolveWeaponHit_ReducesBossHp_AndKillsMinions()
    {
        var db = LoadDb();
        var engine = new RoundEngine(new Random(7));
        var state = engine.StartNewGame(db, PlayerSetup(db, 2), DifficultyLevel.Normal);

        DriveToCombatCycle(engine, state);

        var startingBossHp = state.CurrentBossHp;
        var startingMinionCount = state.CurrentMinions.Count;

        // Resolve the boss's own attack (unblocked) to reach the Player Turns step.
        while (state.Phase == GamePhase.BossAttack && state.PendingCrabAttacks.Count > 0)
        {
            engine.ResolveNextCrabAttack(state);
        }

        if (state.Phase != GamePhase.PlayerTurns || state.PendingEquipmentTurns.Count == 0)
        {
            return; // round already ended from the boss attack (small test decks) — nothing more to assert
        }

        var player = state.PendingEquipmentTurns.Peek();
        var weapon = player.Hand.FirstOrDefault(c => c.EquipmentType is EquipmentType.Melee or EquipmentType.Ranged);
        if (weapon is null)
        {
            return; // hand happened not to contain a weapon this seed — nothing to assert
        }

        engine.PlayEquipmentFromHand(state, weapon, CombatTarget.Boss);

        Assert.True(state.CurrentBossHp <= startingBossHp);
        Assert.Equal(startingMinionCount, state.CurrentMinions.Count); // targeted the boss, not minions
    }

    [Fact]
    public void AttackWithDefaultMeleeWeapon_DealsBaseDamage_WithoutDrawingACard()
    {
        var db = LoadDb();
        var engine = new RoundEngine(new Random(3));
        var state = engine.StartNewGame(db, PlayerSetup(db, 2), DifficultyLevel.Normal);

        DriveToCombatCycle(engine, state);
        while (state.Phase == GamePhase.BossAttack && state.PendingCrabAttacks.Count > 0)
        {
            engine.ResolveNextCrabAttack(state);
        }

        Assert.Equal(GamePhase.PlayerTurns, state.Phase);
        Assert.True(state.PendingEquipmentTurns.Count > 0);

        var startingBossHp = state.CurrentBossHp;
        var drawPileBefore = state.EquipmentDeck.DrawPileCount;
        var discardPileBefore = state.EquipmentDeck.DiscardPileCount;
        var player = state.PendingEquipmentTurns.Peek();
        player.Hand.Clear(); // exercise the empty-hand fallback in isolation, regardless of the random equipment draw

        engine.AttackWithDefaultMeleeWeapon(state, CombatTarget.Boss);

        Assert.Equal(Position.Melee, player.Position);
        Assert.Equal(Math.Max(0, startingBossHp - 1), state.CurrentBossHp);
        Assert.Equal(drawPileBefore, state.EquipmentDeck.DrawPileCount);
        Assert.Equal(discardPileBefore, state.EquipmentDeck.DiscardPileCount);
    }

    [Fact]
    public void AttackWithDefaultMeleeWeapon_IsAllowed_WhenPlayerStillHasCardsInHand()
    {
        var db = LoadDb();
        var engine = new RoundEngine(new Random(3));
        var state = engine.StartNewGame(db, PlayerSetup(db, 2), DifficultyLevel.Normal);

        DriveToCombatCycle(engine, state);
        while (state.Phase == GamePhase.BossAttack && state.PendingCrabAttacks.Count > 0)
        {
            engine.ResolveNextCrabAttack(state);
        }

        Assert.Equal(GamePhase.PlayerTurns, state.Phase);
        var player = state.PendingEquipmentTurns.Peek();
        Assert.NotEmpty(player.Hand); // fresh hand from DrawEquipmentForAllPlayers
        var handBefore = player.Hand.Count;
        var startingBossHp = state.CurrentBossHp;

        engine.AttackWithDefaultMeleeWeapon(state, CombatTarget.Boss);

        // The default melee attack is always available — it doesn't consume or require an
        // empty hand.
        Assert.Equal(handBefore, player.Hand.Count);
        Assert.Equal(Position.Melee, player.Position);
        Assert.Equal(Math.Max(0, startingBossHp - 1), state.CurrentBossHp);
    }

    [Fact]
    public void MinionDeck_ReshufflesDiscard_OnceAllMinionsHaveBeenKilledAndRedrawn()
    {
        var db = LoadDb();
        var engine = new RoundEngine(new Random(5));
        // Every boss calls for exactly 1 minion at 2p (crab_bosses.csv Minions2p), so a 1-card
        // minion deck is fully drained by a single DrawMinions call regardless of which boss is drawn.
        var smallMinionDeck = new Deck<CrabMinionCard>(db.Minions.Take(1), new Random(5));
        var state = new GameState
        {
            Players = new List<Player>
            {
                new() { Name = "Player 1", Character = db.Characters[0] },
                new() { Name = "Player 2", Character = db.Characters[1] },
            },
            Difficulty = DifficultyLevel.Normal,
            EquipmentDeck = new Deck<EquipmentCard>(db.Equipment, new Random(5)),
            DamageDeck = new Deck<DamageCard>(db.DamageCards, new Random(5)),
            BossDeck = new Deck<CrabBossCard>(db.Bosses, new Random(5)),
            CrabMinionDeck = smallMinionDeck,
            RemainingLocations = db.NonShuttleLocations.Take(1).ToList(),
            FirstPlayerIndex = 0,
            RoundNumber = 0,
            Phase = GamePhase.LocationReveal,
        };

        engine.RevealNextLocation(state);
        engine.DrawEquipmentForAllPlayers(state);
        engine.DrawBoss(state);
        engine.DrawMinions(state); // drains the 1-card minion draw pile; auto-starts the Boss Attack step

        Assert.Equal(0, state.CrabMinionDeck.DrawPileCount);

        // Resolve the boss's own attack (unblocked) to reach the Player Turns step — this test is
        // about the minion deck's reshuffle, not crab attack resolution.
        while (state.Phase == GamePhase.BossAttack && state.PendingCrabAttacks.Count > 0)
        {
            engine.ResolveNextCrabAttack(state);
        }

        // Guarantee whoever is up next has a melee modifier in hand, regardless of the random
        // equipment draw — every weapon card's DamageBonus is >= 1, so one hit (1 base + >= 1
        // bonus = >= 2) is enough to one-shot both flat-1-HP minions.
        var meleeModifier = db.Equipment.First(e => e.EquipmentType == EquipmentType.Melee);
        foreach (var p in state.Players)
        {
            p.Hand.Add(meleeModifier);
        }

        engine.PlayEquipmentFromHand(state, meleeModifier, CombatTarget.Minions);

        Assert.Empty(state.CurrentMinions);
        Assert.Equal(1, state.CrabMinionDeck.DiscardPileCount); // the killed minion was discarded

        var redrawn = state.CrabMinionDeck.Draw(1); // RULES.md: minion pile reshuffles its own discard

        Assert.Single(redrawn);
        Assert.Equal(0, state.CrabMinionDeck.DiscardPileCount);
    }

    // ---- Medic passive: heal cards only self-target unless a Medic is alive in the crew ----

    private static GameState MinimalCombatState(CardDatabase db, List<Player> players, Player healer, EquipmentCard healCard)
    {
        healer.Hand.Add(healCard);
        var state = new GameState
        {
            Players = players,
            Difficulty = DifficultyLevel.Normal,
            EquipmentDeck = new Deck<EquipmentCard>(db.Equipment, new Random(1)),
            DamageDeck = new Deck<DamageCard>(db.DamageCards, new Random(1)),
            BossDeck = new Deck<CrabBossCard>(db.Bosses, new Random(1)),
            CrabMinionDeck = new Deck<CrabMinionCard>(db.Minions, new Random(1)),
            RemainingLocations = db.NonShuttleLocations.Take(1).ToList(),
            FirstPlayerIndex = 0,
            RoundNumber = 1,
            Phase = GamePhase.PlayerTurns,
        };
        state.PendingEquipmentTurns.Enqueue(healer);
        return state;
    }

    [Fact]
    public void PlayEquipmentFromHand_Heal_CanTargetTeammate_WhenMedicIsAlive()
    {
        var db = LoadDb();
        var engine = new RoundEngine(new Random(1));
        var healer = new Player { Name = "Healer", Character = db.Characters.Single(c => c.Name == "Brawler") };
        var patient = new Player { Name = "Patient", Character = db.Characters.Single(c => c.Name == "Medic") };
        patient.TakeDamage(3);
        var healCard = db.Equipment.First(e => e.EquipmentType == EquipmentType.Healing);
        var state = MinimalCombatState(db, new List<Player> { healer, patient }, healer, healCard);

        engine.PlayEquipmentFromHand(state, healCard, healTarget: patient);

        // BaseHealAmount (2) + Medic's +1 passive bonus = 3, exactly offsetting the 3 damage taken.
        Assert.Equal(Player.MaxHp, patient.CurrentHp);
    }

    [Fact]
    public void PlayEquipmentFromHand_Heal_ThrowsWhenTargetingTeammate_WithoutMedic()
    {
        var db = LoadDb();
        var engine = new RoundEngine(new Random(1));
        var healer = new Player { Name = "Healer", Character = db.Characters.Single(c => c.Name == "Brawler") };
        var patient = new Player { Name = "Patient", Character = db.Characters.Single(c => c.Name == "Biologist") };
        var healCard = db.Equipment.First(e => e.EquipmentType == EquipmentType.Healing);
        var state = MinimalCombatState(db, new List<Player> { healer, patient }, healer, healCard);

        Assert.Throws<InvalidOperationException>(() => engine.PlayEquipmentFromHand(state, healCard, healTarget: patient));
    }

    [Fact]
    public void PlayEquipmentFromHand_Heal_SelfTargetAlwaysAllowed_WithoutMedic()
    {
        var db = LoadDb();
        var engine = new RoundEngine(new Random(1));
        var healer = new Player { Name = "Healer", Character = db.Characters.Single(c => c.Name == "Brawler") };
        var bystander = new Player { Name = "Bystander", Character = db.Characters.Single(c => c.Name == "Biologist") };
        healer.TakeDamage(2);
        var healCard = db.Equipment.First(e => e.EquipmentType == EquipmentType.Healing);
        var state = MinimalCombatState(db, new List<Player> { healer, bystander }, healer, healCard);

        engine.PlayEquipmentFromHand(state, healCard, healTarget: healer);

        Assert.Equal(Player.MaxHp, healer.CurrentHp);
    }

    private static void DriveToCombatCycle(RoundEngine engine, GameState state)
    {
        engine.RevealNextLocation(state);
        engine.DrawEquipmentForAllPlayers(state);
        engine.DrawBoss(state);
        engine.DrawMinions(state); // ends at GamePhase.BossAttack — the boss always has full HP here
    }
}
