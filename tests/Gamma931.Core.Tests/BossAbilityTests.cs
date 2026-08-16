using Gamma931.Core.Data;
using Gamma931.Core.Game;
using Gamma931.Core.Models;
using Xunit;

namespace Gamma931.Core.Tests;

/// <summary>
/// Covers the boss abilities wired into RoundEngine from BALANCE_NOTES.md's "Boss Ability
/// Balance Pass". Each test pins the drawn boss and location (so biome-gating is deterministic)
/// and, where the ability rolls a probability, forces that roll via <see cref="AlwaysSucceedRandom"/>
/// or <see cref="AlwaysFailRandom"/> rather than guessing a lucky seed.
/// </summary>
public class BossAbilityTests
{
    /// <summary>Forces every RoundEngine ability probability check (`_random.NextDouble() &lt;
    /// chance`) to succeed, while leaving Next(int) (deck shuffles, target/attacker picks) as
    /// real pseudo-randomness — NextDouble() is the only virtual entry point RoundEngine's
    /// ability rolls use.</summary>
    private sealed class AlwaysSucceedRandom : Random
    {
        public override double NextDouble() => 0.0;
    }

    /// <summary>Forces every ability probability check to fail (every tuned chance in
    /// RoundEngine is well under 1.0).</summary>
    private sealed class AlwaysFailRandom : Random
    {
        public override double NextDouble() => 0.999;
    }

    private static CardDatabase LoadDb() =>
        new CsvCardLoader().LoadFromDirectory(Path.Combine(AppContext.BaseDirectory, "TestData"));

    private static GameState BuildState(
        CardDatabase db, int playerCount, CrabBossCard boss, LocationCard location, Random random,
        IEnumerable<EquipmentCard>? equipmentDeckCards = null,
        IEnumerable<DamageCard>? damageDeckCards = null)
    {
        var players = Enumerable.Range(1, playerCount)
            .Select(i => new Player
            {
                Name = $"Player {i}",
                Character = db.Characters[(i - 1) % db.Characters.Count],
            })
            .ToList();

        return new GameState
        {
            Players = players,
            Difficulty = DifficultyLevel.Normal,
            EquipmentDeck = new Deck<EquipmentCard>(equipmentDeckCards ?? db.Equipment, random),
            DamageDeck = new Deck<DamageCard>(damageDeckCards ?? db.DamageCards, random),
            BossDeck = new Deck<CrabBossCard>(new[] { boss }, random),
            CrabMinionDeck = new Deck<CrabMinionCard>(db.Minions, random),
            RemainingLocations = new List<LocationCard> { location },
            FirstPlayerIndex = 0,
            RoundNumber = 0,
            Phase = GamePhase.LocationReveal,
        };
    }

    /// <summary>Drives through to GamePhase.BossAttack — the boss always has full HP here, so
    /// there's always exactly one pending "Boss" attack queued.</summary>
    private static void DriveToCombatCycle(RoundEngine engine, GameState state)
    {
        engine.RevealNextLocation(state);
        engine.DrawEquipmentForAllPlayers(state);
        engine.DrawBoss(state);
        engine.DrawMinions(state);
    }

    /// <summary>Restricting a test's equipment deck to a card subtype (e.g. "only weapons") makes
    /// the drawn hands deterministic, but a small subtype list can be smaller than a round's total
    /// draw before anything's been discarded to reshuffle from — repeat it generously so Deck&lt;T&gt;
    /// never runs dry mid-draw.</summary>
    private static List<EquipmentCard> RepeatToFillDeck(IEnumerable<EquipmentCard> cards)
    {
        var list = cards.ToList();
        return Enumerable.Repeat(list, 20).SelectMany(c => c).ToList();
    }

    private static LocationCard LocationFor(CardDatabase db, string biome) =>
        db.NonShuttleLocations.Single(l => l.Biome == biome);

    private static CrabBossCard BossNamed(CardDatabase db, string name) =>
        db.Bosses.Single(b => b.Name == name);

    // ---- Broodmother: 15% chance/round to breed 1 extra minion ----

    [Fact]
    public void Broodmother_BreedsExtraMinion_WhenRollSucceeds()
    {
        var db = LoadDb();
        var boss = BossNamed(db, "Broodmother");
        var location = db.NonShuttleLocations[0]; // Universal — biome doesn't matter
        var random = new AlwaysSucceedRandom();
        var engine = new RoundEngine(random);
        var state = BuildState(db, 3, boss, location, random);

        DriveToCombatCycle(engine, state);

        Assert.Equal(boss.MinionCountFor(3) + 1, state.CurrentMinions.Count);
    }

    [Fact]
    public void Broodmother_DoesNotBreed_WhenRollFails()
    {
        var db = LoadDb();
        var boss = BossNamed(db, "Broodmother");
        var location = db.NonShuttleLocations[0];
        var random = new AlwaysFailRandom();
        var engine = new RoundEngine(random);
        var state = BuildState(db, 3, boss, location, random);

        DriveToCombatCycle(engine, state);

        Assert.Equal(boss.MinionCountFor(3), state.CurrentMinions.Count);
    }

    // ---- Sandreaver: minions get a 30% chance of +1 dmg vs. a range-position target ----

    [Fact]
    public void Sandreaver_AddsBonusDamage_ToRangeTarget_WhenActiveAndRollSucceeds()
    {
        var db = LoadDb();
        var boss = BossNamed(db, "Sandreaver");
        var desert = LocationFor(db, "Desert"); // Sandreaver's home biome — ability active
        var random = new AlwaysSucceedRandom();
        var engine = new RoundEngine(random);
        var oneHpCard = db.DamageCards.First(d => !d.ArmsDebuff && d.HpCost == 1); // fixed 1 HP base, no debuff
        var state = BuildState(db, 2, boss, desert, random, damageDeckCards: new[] { oneHpCard });

        engine.RevealNextLocation(state);
        engine.DrawEquipmentForAllPlayers(state);
        engine.DrawBoss(state);

        var target = state.Players[0];
        target.Position = Position.Range;

        engine.ResolveCrabAttack(state, target, blockingCard: null, attackerLabel: "A minion", attackerIsMinion: true);

        Assert.Equal(Player.MaxHp - 2, target.CurrentHp); // 1 base + 1 Sandreaver bonus
    }

    [Fact]
    public void Sandreaver_DoesNotAddBonusDamage_WhenLocationBiomeDoesNotMatch()
    {
        var db = LoadDb();
        var boss = BossNamed(db, "Sandreaver");
        var swamp = LocationFor(db, "Swamp"); // wrong biome — Sandreaver's ability is inactive here
        var random = new AlwaysSucceedRandom();
        var engine = new RoundEngine(random);
        var oneHpCard = db.DamageCards.First(d => !d.ArmsDebuff && d.HpCost == 1);
        var state = BuildState(db, 2, boss, swamp, random, damageDeckCards: new[] { oneHpCard });

        engine.RevealNextLocation(state);
        engine.DrawEquipmentForAllPlayers(state);
        engine.DrawBoss(state);

        var target = state.Players[0];
        target.Position = Position.Range;

        engine.ResolveCrabAttack(state, target, blockingCard: null, attackerLabel: "A minion", attackerIsMinion: true);

        Assert.Equal(Player.MaxHp - 1, target.CurrentHp); // base damage only, no bonus off-biome
    }

    // ---- Bogfather: heals 1 HP once, right before the crew's first turn, not every cycle ----

    [Fact]
    public void Bogfather_HealsOnceBeforeFirstPlayerTurn_AndDoesNotReheal_OnLaterCycles()
    {
        var db = LoadDb();
        var boss = BossNamed(db, "Bogfather");
        var swamp = LocationFor(db, "Swamp");
        var random = new Random(42);
        var engine = new RoundEngine(random);
        var healingCards = RepeatToFillDeck(db.Equipment.Where(e => e.EquipmentType == EquipmentType.Healing));
        var state = BuildState(db, 3, boss, swamp, random, equipmentDeckCards: healingCards);

        DriveToCombatCycle(engine, state);
        state.CurrentBossHp = boss.HpFor(3) - 3;
        var healedHp = state.CurrentBossHp + 1;

        PlayOutCombatCycle(engine, state);
        Assert.Equal(healedHp, state.CurrentBossHp);
        Assert.True(state.BossAbilityUsedThisRound);

        // A second combat cycle (RULES.md's cycle repeats until the round is over) must not heal again.
        PlayOutCombatCycle(engine, state);
        Assert.Equal(healedHp, state.CurrentBossHp);
    }

    // ---- Frostclaw: 40% chance/round to freeze 1 card out of a random player's hand ----

    [Fact]
    public void Frostclaw_FreezesExactlyOneCard_WhenRollSucceeds()
    {
        var db = LoadDb();
        var boss = BossNamed(db, "Frostclaw");
        var ice = LocationFor(db, "Ice/Tundra");
        var random = new AlwaysSucceedRandom();
        var engine = new RoundEngine(random);
        var state = BuildState(db, 3, boss, ice, random);

        engine.RevealNextLocation(state);
        engine.DrawEquipmentForAllPlayers(state);
        var totalBefore = state.Players.Sum(p => p.Hand.Count);

        engine.DrawBoss(state);

        var totalAfter = state.Players.Sum(p => p.Hand.Count);
        Assert.Equal(totalBefore - 1, totalAfter);
    }

    [Fact]
    public void Frostclaw_FreezesNoCard_WhenRollFails()
    {
        var db = LoadDb();
        var boss = BossNamed(db, "Frostclaw");
        var ice = LocationFor(db, "Ice/Tundra");
        var random = new AlwaysFailRandom();
        var engine = new RoundEngine(random);
        var state = BuildState(db, 3, boss, ice, random);

        engine.RevealNextLocation(state);
        engine.DrawEquipmentForAllPlayers(state);
        var totalBefore = state.Players.Sum(p => p.Hand.Count);

        engine.DrawBoss(state);

        var totalAfter = state.Players.Sum(p => p.Hand.Count);
        Assert.Equal(totalBefore, totalAfter);
    }

    // ---- Vinewarden: regenerates 1 HP/cycle, capped at the first 2 cycles it survives per round ----

    /// <summary>Plays out one full combat cycle — the boss's own attack (if alive), every living
    /// player's equipment turn, then every living minion's attack — taking every crab attack
    /// unblocked and playing hand[0] each turn (or swinging the default melee weapon once a hand
    /// is empty). Stops early if the round ends mid-cycle.</summary>
    private static void PlayOutCombatCycle(RoundEngine engine, GameState state)
    {
        while (state.Phase == GamePhase.BossAttack && state.PendingCrabAttacks.Count > 0)
        {
            engine.ResolveNextCrabAttack(state);
        }

        while (state.Phase == GamePhase.PlayerTurns && state.PendingEquipmentTurns.Count > 0)
        {
            var player = state.PendingEquipmentTurns.Peek();
            if (player.Hand.Count > 0)
            {
                engine.PlayEquipmentFromHand(state, player.Hand[0]);
            }
            else
            {
                engine.AttackWithDefaultMeleeWeapon(state);
            }
        }

        while (state.Phase == GamePhase.MinionAttacks && state.PendingCrabAttacks.Count > 0)
        {
            engine.ResolveNextCrabAttack(state);
        }
    }

    [Fact]
    public void Vinewarden_RegeneratesUpToTwoTicksPerRound_ThenStops()
    {
        var db = LoadDb();
        var boss = BossNamed(db, "Vinewarden");
        var jungle = LocationFor(db, "Jungle");
        var random = new Random(7);
        var engine = new RoundEngine(random);
        var healingCards = RepeatToFillDeck(db.Equipment.Where(e => e.EquipmentType == EquipmentType.Healing));
        var state = BuildState(db, 2, boss, jungle, random, equipmentDeckCards: healingCards);

        DriveToCombatCycle(engine, state);
        state.CurrentBossHp = 1;

        PlayOutCombatCycle(engine, state);
        Assert.Equal(2, state.CurrentBossHp);

        PlayOutCombatCycle(engine, state);
        Assert.Equal(3, state.CurrentBossHp);

        PlayOutCombatCycle(engine, state);
        Assert.Equal(3, state.CurrentBossHp); // cap reached — third cycle does not regenerate
        Assert.Equal(2, state.BossAbilityTicksThisRound);
    }

    // ---- Magmapincer: 30% chance/round, decided on the first hit that lands, to burn the attacker ----

    [Fact]
    public void Magmapincer_BurnsAttacker_OnFirstLandedHit_WhenRollSucceeds()
    {
        var db = LoadDb();
        var boss = BossNamed(db, "Magmapincer");
        var volcanic = LocationFor(db, "Volcanic/Caves");
        var random = new AlwaysSucceedRandom();
        var engine = new RoundEngine(random);
        var weapons = RepeatToFillDeck(db.Equipment.Where(e => e.EquipmentType is EquipmentType.Melee or EquipmentType.Ranged));
        var state = BuildState(db, 3, boss, volcanic, random, equipmentDeckCards: weapons);

        DriveToCombatCycle(engine, state);
        while (state.Phase == GamePhase.BossAttack && state.PendingCrabAttacks.Count > 0)
        {
            engine.ResolveNextCrabAttack(state);
        }

        Assert.Equal(GamePhase.PlayerTurns, state.Phase);
        Assert.True(state.PendingEquipmentTurns.Count > 0);

        var player = state.PendingEquipmentTurns.Peek();
        var weapon = player.Hand[0];
        var startingHp = player.CurrentHp;

        engine.PlayEquipmentFromHand(state, weapon, CombatTarget.Boss);

        Assert.Equal(startingHp - 1, player.CurrentHp);
    }

    // ---- Wreckstalker: 25% chance per boss attack to ambush the round's first player directly ----

    [Fact]
    public void Wreckstalker_AmbushesFirstPlayer_WhenRollSucceeds()
    {
        var db = LoadDb();
        var boss = BossNamed(db, "Wreckstalker");
        var ruins = LocationFor(db, "Ruins/Wreckage");
        var random = new AlwaysSucceedRandom();
        var engine = new RoundEngine(random);
        var state = BuildState(db, 4, boss, ruins, random);

        DriveToCombatCycle(engine, state);

        // Put a different player in melee — normal priority would target them instead of the
        // first player, so this proves the ambush actually overrides the usual priority order.
        state.Players[1].Position = Position.Melee;

        state.Phase = GamePhase.BossAttack;
        state.PendingCrabAttacks.Clear();
        state.PendingCrabAttacks.Enqueue("Boss");

        engine.ResolveNextCrabAttack(state);

        Assert.True(state.Players[0].CurrentHp < Player.MaxHp);
        Assert.Equal(Player.MaxHp, state.Players[1].CurrentHp);
    }

    // ---- Tideshell: first tier-2+ hit/round has a 40% chance to spawn 1 extra minion ----

    [Fact]
    public void Tideshell_SplitsIntoExtraMinion_WhenTierTwoHitLandsAndRollSucceeds()
    {
        var db = LoadDb();
        var boss = BossNamed(db, "Tideshell");
        var reef = LocationFor(db, "Coastal/Reef");
        var random = new AlwaysSucceedRandom();
        var engine = new RoundEngine(random);
        var tierTwoWeapons = RepeatToFillDeck(db.Equipment.Where(e => e.DamageBonus >= 2));
        var state = BuildState(db, 3, boss, reef, random, equipmentDeckCards: tierTwoWeapons);

        DriveToCombatCycle(engine, state);
        var startingMinionCount = state.CurrentMinions.Count;

        while (state.Phase == GamePhase.BossAttack && state.PendingCrabAttacks.Count > 0)
        {
            engine.ResolveNextCrabAttack(state);
        }

        var player = state.PendingEquipmentTurns.Peek();
        var weapon = player.Hand[0];
        engine.PlayEquipmentFromHand(state, weapon, CombatTarget.Boss);

        Assert.Equal(startingMinionCount + 1, state.CurrentMinions.Count);
    }

    // ---- Ironshell: no ability beyond its higher 8 HP, already loaded from crab_bosses.csv ----

    [Fact]
    public void Ironshell_HasNoAbility_JustHigherStartingHp()
    {
        var db = LoadDb();
        var boss = BossNamed(db, "Ironshell");
        var alphaDrone = BossNamed(db, "Alpha Drone");

        // Ironshell's whole gimmick is a flat +2 HP over the baseline boss (Alpha Drone), held
        // consistently across every player count.
        foreach (var playerCount in new[] { 2, 3, 4, 5 })
        {
            Assert.Equal(alphaDrone.HpFor(playerCount) + 2, boss.HpFor(playerCount));
        }

        Assert.Equal(8, boss.HpFor(2));
        Assert.DoesNotContain("chance", boss.AbilityText, StringComparison.OrdinalIgnoreCase);
    }
}
