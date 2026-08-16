using Gamma931.Core.Data;
using Gamma931.Core.Models;

namespace Gamma931.Core.Game;

/// <summary>
/// Drives a <see cref="GameState"/> through the round/combat loop from RULES.md, one explicit
/// step at a time. Each public method corresponds to a step a human would take at the table
/// (reveal a location, draw equipment, play a card, resolve an attack...) so the WPF UI can
/// present the round structure interactively instead of auto-simulating it.
///
/// Several numeric values (weapon damage, heal amounts) are not pinned down anywhere in
/// RULES.md/ROSTER.md yet — those are marked below and use clearly-placeholder defaults meant to
/// be tuned once real playtesting starts. All 10 crab_bosses.csv abilities are wired up (their
/// magnitudes came from tools/boss_ability_simulation.py — see BALANCE_NOTES.md), as is Medic's
/// passive/active. Most *character* active abilities besides Medic's are still "TBD" in
/// ROSTER.md and not simulated; Medic remains the template for wiring up the rest of the roster.
/// </summary>
public sealed class RoundEngine
{
    private const int BaseHealAmount = 2; // TODO placeholder: not numerically specified in RULES.md
    private const string MedicCharacterName = "Medic";

    // RULES.md "Weapons & Modifiers": every player always has a default melee weapon and a
    // default ranged weapon, each dealing this much on its own. Playing a Melee/Ranged equipment
    // card is playing a weapon modifier (melee) or ammo (ranged) that adds its DamageBonus on top.
    private const int BaseWeaponDamage = 1;

    // Boss ability magnitudes below come from tools/boss_ability_simulation.py's Monte Carlo
    // tuning pass (see BALANCE_NOTES.md "Boss Ability Balance Pass") — chosen so drawing any one
    // boss is roughly as threatening as any other, not picked by feel. Biome-specific bosses only
    // get their ability when CrabBossCard.IsActiveAt the current location's biome; see
    // CurrentBossAbilityActive.
    private const string BroodmotherName = "Broodmother";
    private const string SandreaverName = "Sandreaver";
    private const string BogfatherName = "Bogfather";
    private const string FrostclawName = "Frostclaw";
    private const string VinewardenName = "Vinewarden";
    private const string MagmapincerName = "Magmapincer";
    private const string WreckstalkerName = "Wreckstalker";
    private const string TideshellName = "Tideshell";

    private const double BroodmotherExtraMinionChance = 0.15;
    private const double SandreaverBonusDamageChance = 0.30;
    private const int BogfatherHealAmount = 1;
    private const double FrostclawFreezeChance = 0.40;
    private const int VinewardenRegenPerTick = 1;
    private const int VinewardenMaxRegenTicksPerRound = 2;
    private const double MagmapincerBurnChance = 0.30;
    private const int MagmapincerBurnDamage = 1;
    private const double WreckstalkerAmbushChance = 0.25;
    private const double TideshellSplitChance = 0.40;
    private const int TideshellSplitMinTier = 2;
    private const int TideshellSplitMinionsSpawned = 1;

    private readonly Random _random;

    public RoundEngine(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public GameState StartNewGame(
        CardDatabase db,
        IReadOnlyList<(string PlayerName, CharacterCard Character)> playerSetup,
        DifficultyLevel difficulty)
    {
        if (playerSetup.Count is < 2 or > 5)
        {
            throw new ArgumentException("Gamma-931 supports 2-5 players.", nameof(playerSetup));
        }

        var players = playerSetup.Select(p => new Player
        {
            Name = p.PlayerName,
            Character = p.Character,
            ActiveUsesRemaining = p.Character.ActiveUsesFor(difficulty),
        }).ToList();

        var chosenLocations = db.NonShuttleLocations.OrderBy(_ => _random.Next()).Take(5).ToList();
        chosenLocations.Add(db.ShuttleLocation);

        var state = new GameState
        {
            Players = players,
            Difficulty = difficulty,
            EquipmentDeck = new Deck<EquipmentCard>(db.Equipment, _random),
            DamageDeck = new Deck<DamageCard>(db.DamageCards, _random),
            BossDeck = new Deck<CrabBossCard>(db.Bosses, _random),
            CrabActionDeck = new Deck<CrabActionCard>(db.CrabActions, _random),
            CrabMinionDeck = new Deck<CrabMinionCard>(db.Minions, _random),
            RemainingLocations = chosenLocations,
            FirstPlayerIndex = 0,
            RoundNumber = 0,
            Phase = GamePhase.LocationReveal,
        };

        state.LogEvent($"New {playerSetup.Count}-player game started on {difficulty} difficulty.");
        return state;
    }

    /// <summary>Round Structure step 1: reveal the next location card. Ends the game if it's the shuttle.</summary>
    public void RevealNextLocation(GameState state)
    {
        RequirePhase(state, GamePhase.LocationReveal);

        if (state.RemainingLocations.Count == 0)
        {
            throw new InvalidOperationException("No locations left to reveal.");
        }

        var next = state.RemainingLocations[0];
        state.RemainingLocations.RemoveAt(0);
        state.CurrentLocation = next;

        if (next.IsShuttle)
        {
            state.Phase = GamePhase.Won;
            state.LogEvent("The shuttle has been reached. The crew wins!");
            return;
        }

        state.RoundNumber++;
        foreach (var player in state.Players)
        {
            player.ResetForNewRound();
        }

        state.BossAbilityUsedThisRound = false;
        state.BossAbilityTicksThisRound = 0;

        state.LogEvent($"Round {state.RoundNumber}: location revealed — {next.Name} ({next.Biome}).");
        state.Phase = GamePhase.EquipmentDraw;
    }

    /// <summary>Round Structure step 2: each player draws the location's equipment allowance.</summary>
    public void DrawEquipmentForAllPlayers(GameState state)
    {
        RequirePhase(state, GamePhase.EquipmentDraw);
        var location = RequireCurrentLocation(state);
        var count = location.EquipmentDrawFor(state.Players.Count);

        foreach (var player in state.Players)
        {
            player.Hand.AddRange(state.EquipmentDeck.Draw(count));
        }

        state.LogEvent($"Each player draws {count} equipment card(s).");
        state.Phase = GamePhase.BossReveal;
    }

    /// <summary>Round Structure step 3: draw the boss from the single shared boss deck.</summary>
    public void DrawBoss(GameState state)
    {
        RequirePhase(state, GamePhase.BossReveal);
        var location = RequireCurrentLocation(state);

        var boss = state.BossDeck.Draw();
        state.CurrentBoss = boss;
        state.CurrentBossHp = boss.StartingHp;

        var activeNote = boss.IsActiveAt(location.Biome)
            ? " — its biome bonus is active here."
            : " — biome bonus inactive at this location.";
        state.LogEvent($"Boss revealed: {boss.Name}{activeNote}");

        // Frostclaw: 40% chance/round to freeze one already-drawn card out of a random player's
        // hand (RULES.md equipment is drawn before the boss is revealed, so "reduces equipment
        // draw" has to act on the hand after the fact rather than the draw count itself).
        if (CurrentBossAbilityActive(state, FrostclawName) && _random.NextDouble() < FrostclawFreezeChance)
        {
            var candidates = state.Players.Where(p => p.IsAlive && p.Hand.Count > 0).ToList();
            if (candidates.Count > 0)
            {
                var victim = candidates[_random.Next(candidates.Count)];
                var frozen = victim.Hand[_random.Next(victim.Hand.Count)];
                victim.Hand.Remove(frozen);
                state.EquipmentDeck.Discard(frozen);
                state.LogEvent($"Frostclaw's chill freezes {frozen.Name} out of {victim.Name}'s hand.");
            }
        }

        state.Phase = GamePhase.CrabActionSetAside;
    }

    /// <summary>Round Structure step 4: set aside this round's face-down crab action pile.</summary>
    public void SetAsideCrabActions(GameState state)
    {
        RequirePhase(state, GamePhase.CrabActionSetAside);
        var location = RequireCurrentLocation(state);
        var count = location.CrabActionCountFor(state.Players.Count);

        state.CrabActionsThisRound.Clear();
        foreach (var card in state.CrabActionDeck.Draw(count))
        {
            state.CrabActionsThisRound.Enqueue(card);
        }

        state.LogEvent($"{count} crab action card(s) set aside for this round.");
        state.Phase = GamePhase.MinionReveal;
    }

    /// <summary>Round Structure step 5: draw the boss's minions (each a flat 1 HP).</summary>
    public void DrawMinions(GameState state)
    {
        RequirePhase(state, GamePhase.MinionReveal);
        var location = RequireCurrentLocation(state);
        var count = Math.Max(1, location.MinionCountFor(state.Players.Count));

        // Broodmother: 15% chance/round to breed 1 extra minion on top of the location's count.
        if (CurrentBossAbilityActive(state, BroodmotherName) && _random.NextDouble() < BroodmotherExtraMinionChance)
        {
            count += 1;
            state.LogEvent("Broodmother breeds — 1 extra minion joins the fight.");
        }

        state.CurrentMinions.Clear();
        state.CurrentMinions.AddRange(state.CrabMinionDeck.Draw(count));

        state.LogEvent($"{count} minion(s) join the fight: {string.Join(", ", state.CurrentMinions.Select(m => m.Name))}.");
        state.Phase = GamePhase.CombatCycle;
    }

    /// <summary>
    /// Combat Turn Order step 1: draw and reveal a crab action card. Resolving it is what makes
    /// the crabs act each cycle, so this also selects an attack target per the Crab Attack Rules
    /// priority — call <see cref="ResolveCrabAction"/> next to let the target block or take the
    /// hit before equipment turns are queued.
    /// </summary>
    public void DrawCrabAction(GameState state)
    {
        RequirePhase(state, GamePhase.CombatCycle);

        state.CurrentCrabAction = state.CrabActionsThisRound.Count > 0
            ? state.CrabActionsThisRound.Dequeue()
            : null;

        if (state.CurrentCrabAction is { } action)
        {
            var attacker = SelectCrabAttacker(state);
            state.PendingCrabActionAttacker = attacker;
            var target = SelectAttackTargetWithAmbush(state, attacker);
            state.PendingCrabActionTarget = target;
            state.LogEvent($"Crab action: {action.Name} — {action.EffectText} (targeting {target.Name}).");
        }
        else
        {
            state.LogEvent("No crab action cards remain for this round — no attack this cycle.");
            QueueEquipmentTurnsFromHands(state);
            if (state.PendingEquipmentTurns.Count == 0)
            {
                TransitionWhenNoOneCanPlay(state);
            }
        }
    }

    /// <summary>Resolves the attack queued by <see cref="DrawCrabAction"/>, then queues equipment turns.</summary>
    public void ResolveCrabAction(GameState state, EquipmentCard? blockingCard = null)
    {
        RequirePhase(state, GamePhase.CombatCycle);
        var target = state.PendingCrabActionTarget
            ?? throw new InvalidOperationException("No crab action attack is awaiting resolution.");

        var attackerIsMinion = state.PendingCrabActionAttacker == "Minion";
        ResolveCrabAttack(state, target, blockingCard, state.CurrentCrabAction?.Name ?? "The crabs", attackerIsMinion);
        state.PendingCrabActionTarget = null;
        state.PendingCrabActionAttacker = null;

        if (CheckRoundEnd(state))
        {
            return;
        }

        QueueEquipmentTurnsFromHands(state);
        if (state.PendingEquipmentTurns.Count == 0)
        {
            TransitionWhenNoOneCanPlay(state);
        }
    }

    /// <summary>Combat Turn Order steps 2-3: the next queued player plays one equipment card from hand.</summary>
    public void PlayEquipmentFromHand(GameState state, EquipmentCard card, CombatTarget? target = null, Player? healTarget = null)
    {
        RequirePhase(state, GamePhase.CombatCycle);
        if (state.PendingCrabActionTarget is not null)
        {
            throw new InvalidOperationException("Resolve the pending crab action attack (ResolveCrabAction) before playing equipment.");
        }

        var player = RequireNextEquipmentPlayer(state);

        if (card.EquipmentType == EquipmentType.Protection)
        {
            throw new InvalidOperationException(
                $"{card.Name} is a Protection card — it's held and played as an out-of-turn response " +
                "to a crab attack (see ResolveCrabAttack), not during the normal equipment turn.");
        }

        if (!player.Hand.Remove(card))
        {
            throw new InvalidOperationException($"{player.Name} does not have {card.Name} in hand.");
        }

        state.PendingEquipmentTurns.Dequeue();
        ResolvePlayedEquipment(state, player, card, target, healTarget);
        state.EquipmentDeck.Discard(card);

        if (CheckRoundEnd(state))
        {
            return;
        }

        if (state.PendingEquipmentTurns.Count == 0)
        {
            TransitionWhenNoOneCanPlay(state);
        }
    }

    /// <summary>
    /// Resolves a crab attack against <paramref name="target"/>. RULES.md: "Crab attacks can be
    /// blocked by playing a protection equipment card as a response, out of turn." Pass the
    /// player's chosen Protection card to block, or null to take the hit.
    /// </summary>
    public void ResolveCrabAttack(
        GameState state, Player target, EquipmentCard? blockingCard = null, string attackerLabel = "A crab",
        bool attackerIsMinion = false)
    {
        if (!target.IsAlive)
        {
            throw new InvalidOperationException($"{target.Name} is already down and cannot be targeted.");
        }

        if (blockingCard is not null)
        {
            if (blockingCard.EquipmentType != EquipmentType.Protection || !target.Hand.Contains(blockingCard))
            {
                throw new InvalidOperationException($"{target.Name} cannot block with {blockingCard.Name}.");
            }

            target.Hand.Remove(blockingCard);
            state.EquipmentDeck.Discard(blockingCard);
            state.LogEvent($"{attackerLabel} attacks {target.Name} — blocked with {blockingCard.Name}!");
            return;
        }

        var damageCard = state.DamageDeck.Draw();
        state.DamageDeck.Discard(damageCard);
        var amount = damageCard.HpCost;

        // Sandreaver: minions have a 30% chance of +1 bonus damage against a range-position
        // target. Boss attacks aren't affected — only its minions get the heat-seeking buff.
        if (attackerIsMinion
            && target.Position == Position.Range
            && CurrentBossAbilityActive(state, SandreaverName)
            && _random.NextDouble() < SandreaverBonusDamageChance)
        {
            amount += 1;
            state.LogEvent("Sandreaver's burrowing minions strike with +1 bonus damage!");
        }

        target.TakeDamage(amount);
        state.LogEvent(
            $"{attackerLabel} attacks {target.Name}: {damageCard.BodyLocation} hit for {amount} HP " +
            $"(now {target.CurrentHp}/{Player.MaxHp}).");

        if (!target.IsAlive)
        {
            state.LogEvent($"{target.Name} has fallen!");
        }
    }

    /// <summary>Crab Attack Rules: melee-position player, else range-position player, else the round's first player.</summary>
    public Player SelectCrabAttackTarget(GameState state)
    {
        var alive = state.Players.Where(p => p.IsAlive).ToList();
        if (alive.Count == 0)
        {
            throw new InvalidOperationException("No players remain to target.");
        }

        var melee = alive.Where(p => p.Position == Position.Melee).ToList();
        if (melee.Count > 0)
        {
            return melee[_random.Next(melee.Count)];
        }

        var range = alive.Where(p => p.Position == Position.Range).ToList();
        if (range.Count > 0)
        {
            return range[_random.Next(range.Count)];
        }

        return alive.Contains(state.Players[state.FirstPlayerIndex])
            ? state.Players[state.FirstPlayerIndex]
            : alive[0];
    }

    /// <summary>Entered when hands run dry with crabs still alive. Starts the End Phase draw-from-deck pass.</summary>
    public void BeginEndPhaseCombat(GameState state)
    {
        state.Phase = GamePhase.EndPhaseCombat;

        // Bogfather: heals 1 HP once per round, right before End Phase Combat begins. This method
        // re-runs every End Phase iteration (RULES.md's "repeat until the round is over"), so the
        // once-per-round latch matters — without it this would heal every iteration.
        if (!state.BossAbilityUsedThisRound && CurrentBossAbilityActive(state, BogfatherName) && state.CurrentBossHp > 0)
        {
            state.CurrentBossHp = Math.Min(state.CurrentBoss!.StartingHp, state.CurrentBossHp + BogfatherHealAmount);
            state.BossAbilityUsedThisRound = true;
            state.LogEvent($"Bogfather passively heals {BogfatherHealAmount} HP (now {state.CurrentBossHp}/{state.CurrentBoss.StartingHp}).");
        }

        state.LogEvent("Equipment hands exhausted — entering End Phase Combat.");
        state.PendingEquipmentTurns.Clear();
        foreach (var player in PlayersInTurnOrderFrom(state, state.FirstPlayerIndex).Where(p => p.IsAlive))
        {
            state.PendingEquipmentTurns.Enqueue(player);
        }

        if (state.PendingEquipmentTurns.Count == 0)
        {
            BeginCrabAttackWave(state);
        }
    }

    /// <summary>End Phase Combat step 2: the next queued player draws and immediately resolves the top equipment card.</summary>
    public void EndPhaseDrawAndResolve(GameState state, CombatTarget? target = null, Player? healTarget = null)
    {
        RequirePhase(state, GamePhase.EndPhaseCombat);
        if (state.PendingEquipmentTurns.Count == 0)
        {
            throw new InvalidOperationException("No players left to draw this End Phase pass.");
        }

        var player = state.PendingEquipmentTurns.Dequeue();
        var card = state.EquipmentDeck.Draw();
        state.LogEvent($"{player.Name} draws {card.Name} in End Phase Combat.");

        if (card.EquipmentType == EquipmentType.Protection)
        {
            state.LogEvent($"{card.Name} is a Protection card with no drawn-and-resolved effect; it is discarded.");
        }
        else
        {
            ResolvePlayedEquipment(state, player, card, target, healTarget);
        }

        state.EquipmentDeck.Discard(card);

        if (CheckRoundEnd(state))
        {
            return;
        }

        if (state.PendingEquipmentTurns.Count == 0)
        {
            BeginCrabAttackWave(state);
        }
    }

    /// <summary>Computes (once) and caches the target for the attacker at the front of
    /// <see cref="GameState.PendingCrabAttacks"/>, so the UI can show which player's protection
    /// cards are blockable before the attack resolves. Calling this repeatedly for the same
    /// attacker returns the same cached target rather than re-rolling Wreckstalker's ambush
    /// chance — <see cref="ResolveNextCrabAttack"/> relies on that to resolve against exactly the
    /// player the UI previewed.</summary>
    public Player PeekNextCrabAttackTarget(GameState state)
    {
        if (state.PendingCrabAttackTarget is { } cached)
        {
            return cached;
        }

        if (state.PendingCrabAttacks.Count == 0)
        {
            throw new InvalidOperationException("No crab attacks left in this wave.");
        }

        var target = SelectAttackTargetWithAmbush(state, state.PendingCrabAttacks.Peek());
        state.PendingCrabAttackTarget = target;
        return target;
    }

    /// <summary>End Phase Combat step 3: the next queued crab (boss or a minion) attacks.</summary>
    public void ResolveNextCrabAttack(GameState state, EquipmentCard? blockingCard = null)
    {
        RequirePhase(state, GamePhase.EndPhaseCrabAttacks);
        if (state.PendingCrabAttacks.Count == 0)
        {
            throw new InvalidOperationException("No crab attacks left in this wave.");
        }

        var target = PeekNextCrabAttackTarget(state);
        var attacker = state.PendingCrabAttacks.Dequeue();
        state.PendingCrabAttackTarget = null;
        ResolveCrabAttack(
            state, target, blockingCard,
            attacker == "Boss" ? $"{state.CurrentBoss?.Name ?? "The boss"}" : "A minion",
            attackerIsMinion: attacker == "Minion");

        if (CheckRoundEnd(state))
        {
            return;
        }

        if (state.PendingCrabAttacks.Count == 0)
        {
            // RULES.md End Phase Combat step 4: repeat until the round is over.
            BeginEndPhaseCombat(state);
        }
    }

    /// <summary>Call once Phase is RoundEnd to rotate the first player and clean up for the next location.</summary>
    public void AdvanceToNextRound(GameState state)
    {
        RequirePhase(state, GamePhase.RoundEnd);

        foreach (var player in state.Players)
        {
            if (player.Hand.Count > 0)
            {
                state.EquipmentDeck.Discard(player.Hand);
                player.Hand.Clear();
            }
        }

        state.CurrentBoss = null;
        state.CurrentBossHp = 0;
        state.CurrentMinions.Clear();
        state.CurrentCrabAction = null;

        state.FirstPlayerIndex = (state.FirstPlayerIndex + 1) % state.Players.Count;
        state.Phase = GamePhase.LocationReveal;
        state.LogEvent("Round complete. Advancing to the next location.");
    }

    // ---- internals ----

    private void ResolvePlayedEquipment(GameState state, Player player, EquipmentCard card, CombatTarget? target, Player? healTarget)
    {
        if (card.SetsPosition is { } position)
        {
            player.Position = position;
        }

        switch (card.EquipmentType)
        {
            case EquipmentType.Melee:
            case EquipmentType.Ranged:
                ResolveWeaponHit(state, player, card, target ?? CombatTarget.Boss);
                break;
            case EquipmentType.Healing:
                ResolveHeal(state, player, card, healTarget ?? player);
                break;
            case EquipmentType.Utility:
                // TODO: utility effects are card/character-specific and not yet numerically designed.
                state.LogEvent($"{player.Name} plays {card.Name}: {card.EffectText}");
                break;
            case EquipmentType.Protection:
                throw new InvalidOperationException($"{card.Name} should be resolved via ResolveCrabAttack, not here.");
        }
    }

    private void ResolveWeaponHit(GameState state, Player player, EquipmentCard card, CombatTarget target)
    {
        // RULES.md "Weapons & Modifiers": the default weapon always deals BaseWeaponDamage; a
        // played Melee/Ranged card is a modifier/ammo that adds its DamageBonus on top.
        var hits = BaseWeaponDamage + Math.Max(0, card.DamageBonus);
        ApplyWeaponHit(state, player, hits, card.Name, target);
    }

    /// <summary>
    /// RULES.md "Weapons & Modifiers" / End Phase Combat: lets a player swing their default
    /// melee weapon for its flat <see cref="BaseWeaponDamage"/> with no card played or drawn —
    /// the guaranteed fallback action End Phase Combat now offers instead of only drawing a
    /// random top-of-deck equipment card.
    /// </summary>
    public void EndPhaseAttackWithDefaultMeleeWeapon(GameState state, CombatTarget target = CombatTarget.Boss)
    {
        RequirePhase(state, GamePhase.EndPhaseCombat);
        if (state.PendingEquipmentTurns.Count == 0)
        {
            throw new InvalidOperationException("No players left to act this End Phase pass.");
        }

        var player = state.PendingEquipmentTurns.Dequeue();
        player.Position = Position.Melee;
        state.LogEvent($"{player.Name} swings their default melee weapon (no card drawn).");
        ApplyWeaponHit(state, player, BaseWeaponDamage, "their default melee weapon", target);

        if (CheckRoundEnd(state))
        {
            return;
        }

        if (state.PendingEquipmentTurns.Count == 0)
        {
            BeginCrabAttackWave(state);
        }
    }

    private void ApplyWeaponHit(GameState state, Player player, int hits, string weaponName, CombatTarget target)
    {
        if (target == CombatTarget.Boss)
        {
            if (state.CurrentBoss is not { } boss || state.CurrentBossHp <= 0)
            {
                state.LogEvent($"{player.Name} attacks with {weaponName}, but the boss is already down.");
                return;
            }

            state.CurrentBossHp = Math.Max(0, state.CurrentBossHp - hits);
            state.LogEvent(
                $"{player.Name} hits {boss.Name} with {weaponName} for {hits} " +
                $"(boss HP: {state.CurrentBossHp}/{boss.StartingHp}).");

            // Magmapincer: 30% chance/round, decided on the first hit that lands, to burn
            // whoever dealt it for 1 unblockable damage.
            if (!state.BossAbilityUsedThisRound && CurrentBossAbilityActive(state, MagmapincerName))
            {
                state.BossAbilityUsedThisRound = true;
                if (_random.NextDouble() < MagmapincerBurnChance)
                {
                    player.TakeDamage(MagmapincerBurnDamage);
                    state.LogEvent(
                        $"Magmapincer's heat burns {player.Name} for {MagmapincerBurnDamage} HP " +
                        $"(now {player.CurrentHp}/{Player.MaxHp}).");
                }
            }

            // Tideshell: on the first tier-2+ hit that lands each round, 40% chance to split off
            // 1 extra minion immediately.
            if (!state.BossAbilityUsedThisRound && CurrentBossAbilityActive(state, TideshellName) && hits >= TideshellSplitMinTier)
            {
                state.BossAbilityUsedThisRound = true;
                if (_random.NextDouble() < TideshellSplitChance)
                {
                    state.CurrentMinions.AddRange(state.CrabMinionDeck.Draw(TideshellSplitMinionsSpawned));
                    state.LogEvent($"Tideshell splits — {TideshellSplitMinionsSpawned} extra minion(s) join the fight.");
                }
            }
        }
        else
        {
            var kills = Math.Min(hits, state.CurrentMinions.Count);
            for (var i = 0; i < kills; i++)
            {
                var minion = state.CurrentMinions[^1];
                state.CurrentMinions.RemoveAt(state.CurrentMinions.Count - 1);
                state.CrabMinionDeck.Discard(minion);
            }

            state.LogEvent(kills > 0
                ? $"{player.Name} kills {kills} minion(s) with {weaponName}."
                : $"{player.Name} attacks with {weaponName}, but no minions remain to hit.");
        }
    }

    private void ResolveHeal(GameState state, Player player, EquipmentCard card, Player target)
    {
        var amount = BaseHealAmount;

        // Medic passive (ROSTER.md, fully specified): "Heals for +1 HP whenever a heal card is
        // played (by anyone)". This is the template for wiring up the rest of the roster once
        // their still-TBD abilities are designed.
        if (state.Players.Any(p => p.IsAlive && string.Equals(p.Character.Name, MedicCharacterName, StringComparison.OrdinalIgnoreCase)))
        {
            amount += 1;
        }

        target.Heal(amount);
        state.LogEvent($"{player.Name} plays {card.Name}, healing {target.Name} for {amount} HP (now {target.CurrentHp}/{Player.MaxHp}).");
    }

    private void QueueEquipmentTurnsFromHands(GameState state)
    {
        state.PendingEquipmentTurns.Clear();
        foreach (var player in PlayersInTurnOrderFrom(state, state.FirstPlayerIndex))
        {
            if (player.IsAlive && player.Hand.Count > 0)
            {
                state.PendingEquipmentTurns.Enqueue(player);
            }
        }
    }

    /// <summary>Called exactly once whenever a combat cycle's equipment turns have all been
    /// resolved (immediately, if nobody had a card to play; otherwise after the last one is
    /// played) — i.e. at the true end of the cycle, mirroring
    /// tools/boss_ability_simulation.py's end_of_cycle_regen(), which runs after that cycle's
    /// crab attack and equipment turns rather than before them.</summary>
    private void TransitionWhenNoOneCanPlay(GameState state)
    {
        ApplyVinewardenCycleRegen(state);

        var anyoneStillHasCards = state.Players.Any(p => p.IsAlive && p.Hand.Count > 0);
        if (!anyoneStillHasCards && state.AnyCrabsAlive)
        {
            BeginEndPhaseCombat(state);
        }
    }

    /// <summary>Vinewarden: regenerates 1 HP at the end of each of the first 2 combat cycles it
    /// survives per round (capped — see BALANCE_NOTES.md, an uncapped per-cycle regen was
    /// wildly overtuned).</summary>
    private void ApplyVinewardenCycleRegen(GameState state)
    {
        if (CurrentBossAbilityActive(state, VinewardenName)
            && state.CurrentBossHp > 0
            && state.BossAbilityTicksThisRound < VinewardenMaxRegenTicksPerRound)
        {
            state.CurrentBossHp = Math.Min(state.CurrentBoss!.StartingHp, state.CurrentBossHp + VinewardenRegenPerTick);
            state.BossAbilityTicksThisRound++;
            state.LogEvent($"Vinewarden regrows {VinewardenRegenPerTick} HP (now {state.CurrentBossHp}/{state.CurrentBoss.StartingHp}).");
        }
    }

    private void BeginCrabAttackWave(GameState state)
    {
        state.Phase = GamePhase.EndPhaseCrabAttacks;
        state.PendingCrabAttacks.Clear();
        state.PendingCrabAttackTarget = null;

        if (state.CurrentBossHp > 0)
        {
            state.PendingCrabAttacks.Enqueue("Boss");
        }

        for (var i = 0; i < state.AliveMinionCount; i++)
        {
            state.PendingCrabAttacks.Enqueue("Minion");
        }

        if (state.PendingCrabAttacks.Count == 0)
        {
            CheckRoundEnd(state);
        }
        else
        {
            state.LogEvent("All remaining crabs attack.");
        }
    }

    private bool CheckRoundEnd(GameState state)
    {
        if (!state.AnyPlayersAlive)
        {
            state.Phase = GamePhase.Lost;
            state.LogEvent("All players have died. The crew is lost.");
            return true;
        }

        if (!state.AnyCrabsAlive)
        {
            state.Phase = GamePhase.RoundEnd;
            state.LogEvent($"{state.CurrentLocation?.Name} cleared!");
            return true;
        }

        return false;
    }

    private static IEnumerable<Player> PlayersInTurnOrderFrom(GameState state, int startIndex)
    {
        for (var i = 0; i < state.Players.Count; i++)
        {
            yield return state.Players[(startIndex + i) % state.Players.Count];
        }
    }

    private static Player RequireNextEquipmentPlayer(GameState state)
    {
        if (state.PendingEquipmentTurns.Count == 0)
        {
            throw new InvalidOperationException("No player is currently owed an equipment turn.");
        }

        return state.PendingEquipmentTurns.Peek();
    }

    /// <summary>True if the current boss is <paramref name="bossName"/> and its ability is live —
    /// always true for a Universal boss, only true for a BiomeSpecific one when the current
    /// location's biome matches (CrabBossCard.IsActiveAt).</summary>
    private static bool CurrentBossAbilityActive(GameState state, string bossName)
    {
        var boss = state.CurrentBoss;
        if (boss is null || !string.Equals(boss.Name, bossName, StringComparison.Ordinal))
        {
            return false;
        }

        return boss.IsActiveAt(state.CurrentLocation?.Biome ?? string.Empty);
    }

    /// <summary>Picks which crab (the boss or a minion) is making the next attack, weighted by how
    /// many of each are alive — mirrors tools/boss_ability_simulation.py's crab_pool selection so
    /// attacker-specific abilities (Sandreaver on minions, Wreckstalker on the boss) fire at the
    /// tuned rate during the main combat cycle, not just End Phase Combat.</summary>
    private string SelectCrabAttacker(GameState state)
    {
        var minionCount = state.AliveMinionCount;
        var bossAlive = state.CurrentBossHp > 0;
        if (!bossAlive && minionCount == 0)
        {
            throw new InvalidOperationException("No crabs remain to attack.");
        }

        var bossWeight = bossAlive ? 1 : 0;
        return _random.Next(bossWeight + minionCount) < bossWeight ? "Boss" : "Minion";
    }

    /// <summary>Normal Crab Attack Rules target selection, except Wreckstalker's own attacks have
    /// a 25% chance to ambush the round's first player directly instead of following the usual
    /// melee/range priority.</summary>
    private Player SelectAttackTargetWithAmbush(GameState state, string attacker)
    {
        if (attacker == "Boss"
            && CurrentBossAbilityActive(state, WreckstalkerName)
            && _random.NextDouble() < WreckstalkerAmbushChance)
        {
            var firstPlayer = state.Players[state.FirstPlayerIndex];
            if (firstPlayer.IsAlive)
            {
                return firstPlayer;
            }
        }

        return SelectCrabAttackTarget(state);
    }

    private static LocationCard RequireCurrentLocation(GameState state) =>
        state.CurrentLocation ?? throw new InvalidOperationException("No location has been revealed yet.");

    private static void RequirePhase(GameState state, GamePhase expected)
    {
        if (state.Phase != expected)
        {
            throw new InvalidOperationException($"Expected phase {expected} but game is in {state.Phase}.");
        }
    }
}
