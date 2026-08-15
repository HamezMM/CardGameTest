"""
Monte Carlo tuning pass for crab boss abilities (crab_bosses.csv's AbilityText, currently
all "TBD"). Builds on tools/balance_simulation.py's combat model and the just-tuned
locations.csv minion counts.

Method: turn each boss's one-line ROSTER.md concept into a concrete, numeric ability, then
sweep each ability's magnitude so that boss's difficulty (win rate, boss guaranteed to appear
every round -- an isolated worst-case stress test) lands close to Alpha Drone's baseline (the
boss with literally no ability, at the same minion count) for that player count. The goal
isn't a single global win-rate number like the minion pass -- it's *parity across bosses*, so
no boss in the shared 10-card deck is a wildly harder or easier draw than the others.

Biome-specific bosses (7 of the 10) only get their ability active when the current location's
biome matches theirs (CrabBossCard.IsActiveAt in the C# model) -- everywhere else they behave
like a plain HP-only boss. This script's "always active" test is therefore a deliberate
worst case: real average difficulty is softer than what's tuned here, since the ability only
triggers on a biome match. That's intentional headroom, not an error.

First pass (see boss_ability_simulation_v1_log.txt) found that "every single trigger" designs
(extra minion every round, burn on every hit, hand-reduction for the whole party, regen every
cycle) are catastrophically strong given how steep the win-rate curve already is near the 70%
target -- a flat +1 minion/round alone costs ~26pp of win rate on average. This version
replaces those with probabilistic / capped / single-player-scoped triggers, which is a more
honest reading of "buff, don't overload enemies with rules" from RULES.md's Character Design
philosophy applied to bosses too.
"""

import random
from balance_simulation import (
    PLAYER_HP, EQUIP_DRAW, EQUIPMENT_DECK, BASE_WEAPON_DAMAGE, draw_damage, pick_target,
    HEAL_AMOUNT, END_PHASE_MAX_ITERS,
)

REF_MINIONS = {2: 1, 3: 4, 4: 7, 5: 8}


def crab_attack(rng, hp, positions, protection, boss_alive, minions, first_player,
                 range_bonus_dmg=0, ambush_prob=0.0):
    alive_idxs = [i for i in range(len(hp)) if hp[i] > 0]
    if not alive_idxs:
        return
    crab_pool = (["boss"] if boss_alive else []) + ["minion"] * minions
    if not crab_pool:
        return
    attacker = rng.choice(crab_pool)

    if attacker == "boss" and ambush_prob > 0 and rng.random() < ambush_prob:
        target = first_player if hp[first_player] > 0 else rng.choice(alive_idxs)
    else:
        target = pick_target(rng, alive_idxs, positions)

    dmg = draw_damage(rng)
    if attacker == "minion" and range_bonus_dmg and positions[target] == "range":
        dmg += range_bonus_dmg

    if protection[target] > 0:
        protection[target] -= 1
        return
    hp[target] = max(0, hp[target] - dmg)


def simulate_round(rng, n_players, hp, positions, minion_count, boss, first_player=0):
    """boss: dict with keys hp, ability, param (ability-specific)."""
    boss_max_hp = boss["hp"]
    boss_hp = boss_max_hp
    ability = boss["ability"]
    param = boss.get("param")

    minions = minion_count
    if ability == "extra_minion_chance" and rng.random() < param:
        minions += 1

    equip_draw_n = EQUIP_DRAW[n_players]
    frozen_player = None
    if ability == "freeze_one_player":
        frozen_player = rng.randrange(n_players)

    protection = [0] * n_players
    hands = []
    for p in range(n_players):
        n = equip_draw_n - 1 if (ability == "freeze_one_player" and p == frozen_player) else equip_draw_n
        n = max(1, n)
        hands.append([random.choice(EQUIPMENT_DECK) for _ in range(n)])
    hand_ptr = [0] * n_players

    tideshell_used = False
    magma_burn_used = False
    vine_regen_ticks = 0

    range_bonus_dmg = param if ability == "range_bonus_dmg" else 0
    ambush_prob = param if ability == "ambush_probability" else 0.0

    def resolve_card(card, actor):
        nonlocal boss_hp, minions, tideshell_used, magma_burn_used
        ctype, bonus = card
        if ctype == "Melee":
            positions[actor] = "melee"
        elif ctype == "Ranged":
            positions[actor] = "range"

        if ctype in ("Melee", "Ranged"):
            if minions > 0:
                minions -= 1
            else:
                hits = BASE_WEAPON_DAMAGE + bonus
                dealt = min(hits, boss_hp)
                boss_hp = max(0, boss_hp - hits)
                if dealt > 0 and ability == "burn_once_per_round" and not magma_burn_used:
                    hp[actor] = max(0, hp[actor] - param)
                    magma_burn_used = True
                if dealt > 0 and ability == "split_on_heavy_hit" and not tideshell_used and hits >= param[0]:
                    minions += param[1]
                    tideshell_used = True
        elif ctype == "Healing":
            alive = [i for i in range(n_players) if hp[i] > 0]
            if alive:
                t = min(alive, key=lambda i: hp[i])
                hp[t] = min(PLAYER_HP, hp[t] + HEAL_AMOUNT)
        elif ctype == "Protection":
            protection[actor] += 1
        # Utility: no-op

    def end_of_cycle_regen():
        nonlocal boss_hp, vine_regen_ticks
        if ability == "regen_capped" and boss_hp > 0 and vine_regen_ticks < param[1]:
            boss_hp = min(boss_max_hp, boss_hp + param[0])
            vine_regen_ticks += 1

    cycles = equip_draw_n
    for _ in range(cycles):
        if boss_hp <= 0 and minions <= 0:
            return True
        if all(h <= 0 for h in hp):
            return False

        crab_attack(rng, hp, positions, protection, boss_hp > 0, minions, first_player,
                    range_bonus_dmg, ambush_prob)
        if all(h <= 0 for h in hp):
            return False

        for p in range(n_players):
            if boss_hp <= 0 and minions <= 0:
                break
            if hp[p] <= 0 or hand_ptr[p] >= len(hands[p]):
                continue
            card = hands[p][hand_ptr[p]]
            hand_ptr[p] += 1
            if card[0] == "Protection":
                protection[p] += 1
                continue
            resolve_card(card, p)

        end_of_cycle_regen()

    if boss_hp <= 0 and minions <= 0:
        return True

    if ability == "heal_once_before_endphase" and boss_hp > 0:
        boss_hp = min(boss_max_hp, boss_hp + param)

    for _ in range(END_PHASE_MAX_ITERS):
        if all(h <= 0 for h in hp):
            return False
        for p in range(n_players):
            if boss_hp <= 0 and minions <= 0:
                break
            if hp[p] <= 0:
                continue
            card = random.choice(EQUIPMENT_DECK)
            if card[0] == "Protection":
                protection[p] += 1
                continue
            resolve_card(card, p)
        end_of_cycle_regen()
        if boss_hp <= 0 and minions <= 0:
            return True
        if all(h <= 0 for h in hp):
            return False
        alive_crabs = (["boss"] if boss_hp > 0 else []) + ["minion"] * minions
        for _ in alive_crabs:
            if all(h <= 0 for h in hp):
                return False
            crab_attack(rng, hp, positions, protection, boss_hp > 0, minions, first_player,
                        range_bonus_dmg, ambush_prob)
    return boss_hp <= 0 and minions <= 0


def simulate_game(rng, n_players, boss, minion_count=None):
    mc = minion_count if minion_count is not None else REF_MINIONS[n_players]
    hp = [PLAYER_HP] * n_players
    positions = ["range"] * n_players
    for _ in range(5):
        if not simulate_round(rng, n_players, hp, positions, mc, boss):
            return False
        if all(h <= 0 for h in hp):
            return False
    return True


def win_rate(n_players, boss, trials, seed):
    rng = random.Random(seed)
    wins = sum(simulate_game(rng, n_players, boss) for _ in range(trials))
    return wins / trials


ALPHA_DRONE = {"hp": 6, "ability": None, "param": None}


def baseline():
    return {n: win_rate(n, ALPHA_DRONE, 20000, seed=n) for n in (2, 3, 4, 5)}


def tune(name, ability, hp, param_values, base, trials=10000):
    print(f"\n{name} ({ability}):")
    best = None
    for param in param_values:
        boss = {"hp": hp, "ability": ability, "param": param}
        diffs = []
        line = []
        for n in (3, 4, 5):  # 2p excluded: already floor-locked, see BALANCE_NOTES.md
            wr = win_rate(n, boss, trials, seed=hash((name, str(param), n)) % (2**31))
            diffs.append(abs(wr - base[n]))
            line.append(f"{n}p:{wr*100:5.1f}%(Δ{(wr-base[n])*100:+5.1f})")
        avg_diff = sum(diffs) / len(diffs)
        print(f"  param={param!s:8} " + " ".join(line) + f"  avg|Δ|={avg_diff*100:4.1f}")
        if best is None or avg_diff < best[0]:
            best = (avg_diff, param)
    print(f"  BEST param = {best[1]}")
    return best[1]


if __name__ == "__main__":
    base = baseline()
    print("Alpha Drone baseline (no ability, control):")
    for n in (2, 3, 4, 5):
        print(f"  {n}p: {base[n]*100:.1f}%")

    tune("Broodmother", "extra_minion_chance", 6, [0.15, 0.25, 0.35, 0.5], base)
    tune("Bogfather", "heal_once_before_endphase", 6, [1, 2, 3], base)
    tune("Frostclaw", "freeze_one_player", 6, [True], base)
    tune("Vinewarden", "regen_capped", 6, [(1, 1), (1, 2), (1, 3)], base)
    tune("Magmapincer", "burn_once_per_round", 6, [1, 2, 3], base)
    tune("Sandreaver", "range_bonus_dmg", 6, [1, 2], base)
    tune("Wreckstalker", "ambush_probability", 6, [0.25, 0.5, 0.75, 1.0], base)
    tune("Tideshell", "split_on_heavy_hit", 6, [(2, 1)], base)

    print("\nIronshell (8 HP, no ability) -- for reference:")
    for n in (2, 3, 4, 5):
        boss = {"hp": 8, "ability": None, "param": None}
        wr = win_rate(n, boss, 10000, seed=300 + n)
        print(f"  {n}p: {wr*100:5.1f}%  (baseline {base[n]*100:5.1f}%, Δ{(wr-base[n])*100:+.1f})")

# --- refinement pass: probabilistic variants for the still-too-strong abilities ---

def simulate_round_prob_variants(rng, n_players, hp, positions, minion_count, boss, first_player=0):
    """Same as simulate_round but supports probability-gated freeze/burn/split/range-bonus."""
    boss_max_hp = boss["hp"]
    boss_hp = boss_max_hp
    ability = boss["ability"]
    param = boss.get("param")

    equip_draw_n = EQUIP_DRAW[n_players]
    frozen_player = None
    if ability == "freeze_one_player_prob" and rng.random() < param:
        frozen_player = rng.randrange(n_players)

    protection = [0] * n_players
    hands = []
    for p in range(n_players):
        n = equip_draw_n - 1 if (p == frozen_player) else equip_draw_n
        n = max(1, n)
        hands.append([random.choice(EQUIPMENT_DECK) for _ in range(n)])
    hand_ptr = [0] * n_players
    minions = minion_count

    tideshell_used = False
    magma_burn_used = False
    magma_burn_roll = rng.random() < param if ability == "burn_once_per_round_prob" else False
    range_bonus_roll = ability == "range_bonus_dmg_prob"
    tideshell_roll = rng.random() < param[2] if ability == "split_prob" else False

    def resolve_card(card, actor):
        nonlocal boss_hp, minions, tideshell_used, magma_burn_used
        ctype, bonus = card
        if ctype == "Melee":
            positions[actor] = "melee"
        elif ctype == "Ranged":
            positions[actor] = "range"

        if ctype in ("Melee", "Ranged"):
            if minions > 0:
                minions -= 1
            else:
                hits = BASE_WEAPON_DAMAGE + bonus
                dealt = min(hits, boss_hp)
                boss_hp = max(0, boss_hp - hits)
                if dealt > 0 and ability == "burn_once_per_round_prob" and magma_burn_roll and not magma_burn_used:
                    hp[actor] = max(0, hp[actor] - 1)
                    magma_burn_used = True
                if dealt > 0 and ability == "split_prob" and tideshell_roll and not tideshell_used and hits >= param[0]:
                    minions += param[1]
                    tideshell_used = True
        elif ctype == "Healing":
            alive = [i for i in range(n_players) if hp[i] > 0]
            if alive:
                t = min(alive, key=lambda i: hp[i])
                hp[t] = min(PLAYER_HP, hp[t] + HEAL_AMOUNT)
        elif ctype == "Protection":
            protection[actor] += 1

    def crab_attack_local():
        alive_idxs = [i for i in range(len(hp)) if hp[i] > 0]
        if not alive_idxs:
            return
        crab_pool = (["boss"] if boss_hp > 0 else []) + ["minion"] * minions
        if not crab_pool:
            return
        attacker = rng.choice(crab_pool)
        target = pick_target(rng, alive_idxs, positions)
        dmg = draw_damage(rng)
        if attacker == "minion" and range_bonus_roll and positions[target] == "range" and rng.random() < param:
            dmg += 1
        if protection[target] > 0:
            protection[target] -= 1
            return
        hp[target] = max(0, hp[target] - dmg)

    cycles = equip_draw_n
    for _ in range(cycles):
        if boss_hp <= 0 and minions <= 0:
            return True
        if all(h <= 0 for h in hp):
            return False
        crab_attack_local()
        if all(h <= 0 for h in hp):
            return False
        for p in range(n_players):
            if boss_hp <= 0 and minions <= 0:
                break
            if hp[p] <= 0 or hand_ptr[p] >= len(hands[p]):
                continue
            card = hands[p][hand_ptr[p]]
            hand_ptr[p] += 1
            if card[0] == "Protection":
                protection[p] += 1
                continue
            resolve_card(card, p)

    if boss_hp <= 0 and minions <= 0:
        return True

    for _ in range(END_PHASE_MAX_ITERS):
        if all(h <= 0 for h in hp):
            return False
        for p in range(n_players):
            if boss_hp <= 0 and minions <= 0:
                break
            if hp[p] <= 0:
                continue
            card = random.choice(EQUIPMENT_DECK)
            if card[0] == "Protection":
                protection[p] += 1
                continue
            resolve_card(card, p)
        if boss_hp <= 0 and minions <= 0:
            return True
        if all(h <= 0 for h in hp):
            return False
        alive_crabs = (["boss"] if boss_hp > 0 else []) + ["minion"] * minions
        for _ in alive_crabs:
            if all(h <= 0 for h in hp):
                return False
            crab_attack_local()
    return boss_hp <= 0 and minions <= 0


def simulate_game_prob(rng, n_players, boss, minion_count=None):
    mc = minion_count if minion_count is not None else REF_MINIONS[n_players]
    hp = [PLAYER_HP] * n_players
    positions = ["range"] * n_players
    for _ in range(5):
        if not simulate_round_prob_variants(rng, n_players, hp, positions, mc, boss):
            return False
        if all(h <= 0 for h in hp):
            return False
    return True


def win_rate_prob(n_players, boss, trials, seed):
    rng = random.Random(seed)
    wins = sum(simulate_game_prob(rng, n_players, boss) for _ in range(trials))
    return wins / trials


def tune_prob(name, ability, param_values, base, trials=10000):
    print(f"\n{name} ({ability}) refinement:")
    best = None
    for param in param_values:
        boss = {"hp": 6, "ability": ability, "param": param}
        diffs, line = [], []
        for n in (3, 4, 5):
            wr = win_rate_prob(n, boss, trials, seed=hash((name, str(param), n, "v2")) % (2**31))
            diffs.append(abs(wr - base[n]))
            line.append(f"{n}p:{wr*100:5.1f}%(Δ{(wr-base[n])*100:+5.1f})")
        avg_diff = sum(diffs) / len(diffs)
        print(f"  param={param!s:10} " + " ".join(line) + f"  avg|Δ|={avg_diff*100:4.1f}")
        if best is None or avg_diff < best[0]:
            best = (avg_diff, param)
    print(f"  BEST param = {best[1]}")
    return best[1]


if __name__ == "__main__":
    print("\n\n=== REFINEMENT PASS ===")
    _base = baseline()
    tune_prob("Frostclaw", "freeze_one_player_prob", [0.4, 0.6, 0.8], _base)
    tune_prob("Magmapincer", "burn_once_per_round_prob", [0.3, 0.5, 0.7], _base)
    tune_prob("Sandreaver", "range_bonus_dmg_prob", [0.3, 0.5, 0.7], _base)
    tune_prob("Tideshell", "split_prob", [(2, 1, 0.4), (2, 1, 0.6), (2, 1, 0.8)], _base)
