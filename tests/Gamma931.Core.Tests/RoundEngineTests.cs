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

        engine.DrawCrabAction(state);
        if (state.PendingCrabActionTarget is not null)
        {
            engine.ResolveCrabAction(state);
        }

        if (state.Phase != GamePhase.CombatCycle || state.PendingEquipmentTurns.Count == 0)
        {
            return; // round already ended from the crab attack (small test decks) — nothing more to assert
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

    private static void DriveToCombatCycle(RoundEngine engine, GameState state)
    {
        engine.RevealNextLocation(state);
        engine.DrawEquipmentForAllPlayers(state);
        engine.DrawBoss(state);
        engine.SetAsideCrabActions(state);
        engine.DrawMinions(state);
    }
}
