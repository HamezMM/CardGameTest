"""
Monte Carlo balance simulator for Gamma-931's combat loop.

Purpose: RULES.md leaves several mechanics as literal TBD placeholders (crab action
effects, equipment target selection, heal amounts, utility effects). This script fills
those gaps with explicit, documented assumptions -- see ASSUMPTIONS below -- so that the
*settled* numeric rules (6 HP/player, damage-deck distribution, equipment tiers, boss HP,
equipment/crab-action draw counts already in locations.csv) can be used to solve for the
one number RULES.md explicitly leaves open per player count: how many minion crabs spawn
per location, to hit a target ~70% full-campaign (5-location) win rate.

Run: python3 tools/balance_simulation.py
"""

import random
import statistics

# ---------------------------------------------------------------------------
# Fixed, RULES.md-confirmed numbers
# ---------------------------------------------------------------------------

PLAYER_HP = 6
ROUNDS_PER_GAME = 5  # 5 non-shuttle locations

# locations.csv: EquipmentDrawXp (also used here as combat-cycle count per round,
# since each player's hand is spent one card per cycle and equipment running out is
# what triggers End Phase Combat per RULES.md)
EQUIP_DRAW = {2: 4, 3: 3, 4: 3, 5: 2}

# crab_bosses.csv: 10 bosses, 9 at 6 HP, Ironshell at 8 HP. Drawn uniformly (single
# shared boss deck).
BOSS_HP_POOL = [6, 6, 6, 6, 6, 6, 6, 6, 6, 8]

# equipment.csv: 13-card deck composition (type, DamageBonus). Melee/Ranged cards are weapon
# modifiers/ammo, not stand-alone weapons: RESOLVE below adds this bonus on top of the default
# weapon's flat BASE_WEAPON_DAMAGE (see resolve_card) per RULES.md "Weapons & Modifiers".
# [UPDATED] The 4 former Utility placeholder cards (Grapple Hook, Signal Flare, Emergency
# Rations, Stim Injector -- no modeled effect) were replaced with 2 more Melee weapon upgrades
# and 2 more Ranged ammo cards, so every equipment draw now has combat value.
EQUIPMENT_DECK = (
    [("Melee", 1)] * 3 +      # Sharpened Blade, Honed Point, Vibro Blade
    [("Melee", 2)] * 2 +      # Reinforced Edge, Twin-Edge Cleaver
    [("Ranged", 1)] * 2 +     # Standard Rounds, Scatter Shot
    [("Ranged", 2)] * 2 +     # Armor-Piercing Rounds, Incendiary Rounds
    [("Protection", 0)] * 2 + # Makeshift Shield, Riot Plating
    [("Healing", 0)] * 2      # Field Bandage, Medkit
)
BASE_WEAPON_DAMAGE = 1  # RULES.md: every player's default weapon deals this on its own

# damage.csv: 28-card deck, Arms/Legs/Torso = 1 dmg (24 cards), Head = 2 dmg (4 cards)
DAMAGE_DECK = [1] * 24 + [2] * 4

HEAL_AMOUNT = 2  # equipment-card heal; no character bonuses modeled (see ASSUMPTIONS)
END_PHASE_MAX_ITERS = 25  # safety cap against runaway loops

# ---------------------------------------------------------------------------
# ASSUMPTIONS (filling RULES.md's TBD gaps for simulation purposes only)
# ---------------------------------------------------------------------------
# 1. No character passives/actives modeled. This is a "generic party" baseline --
#    real play with characters (Medic healing, Brawler cleave, Captain's extra draws,
#    etc.) can only make the game *easier* than this baseline, so tuning minion counts
#    against this floor keeps the game beatable even before character balance lands.
# 2. Each crab-action-card draw resolves as exactly one attack from one random alive
#    crab (boss or minion, equal weight) against the priority target (melee position >
#    range position > first player), since all 6 crab_actions.csv effects are literal
#    placeholders.
# 3. Equipment targeting: players always kill an existing minion before damaging the
#    boss (rational play -- fewer attackers left alive), weapon tier only matters
#    against boss HP since minions are flat 1 HP.
# 4. Protection cards are held in reserve and auto-block the next attack against that
#    player (rational play), consumed on use, exactly per "hold and play as a response".
# 5. Healing cards heal the most-injured living player by 2 HP (flat baseline amount;
#    Medic's stated bonus is a character passive, excluded per assumption 1).
# 6. [REMOVED] Utility cards no longer exist in equipment.csv (see EQUIPMENT_DECK above) --
#    every card in the deck now has a modeled effect, so this assumption is moot.
# 7. Equipment hands are sampled i.i.d. from the 13-card deck's proportions rather than
#    tracked as an exact without-replacement shoe across the whole game -- reasonable
#    given RULES.md's own reshuffle-on-exhaustion rule already pushes long-run draws
#    toward the deck's base distribution.
# ---------------------------------------------------------------------------


def draw_damage(rng):
    return rng.choice(DAMAGE_DECK)


def pick_target(rng, alive_idxs, positions):
    melee = [i for i in alive_idxs if positions[i] == "melee"]
    if melee:
        return rng.choice(melee)
    ranged = [i for i in alive_idxs if positions[i] == "range"]
    if ranged:
        return rng.choice(ranged)
    return alive_idxs[0]  # fallback: first (lowest-index) alive player


def crab_attack(rng, hp, positions, protection, boss_alive, minions):
    alive_idxs = [i for i in range(len(hp)) if hp[i] > 0]
    if not alive_idxs:
        return
    crab_pool = (["boss"] if boss_alive else []) + ["minion"] * minions
    if not crab_pool:
        return
    rng.choice(crab_pool)  # which crab attacks doesn't affect outcome, just flavor
    target = pick_target(rng, alive_idxs, positions)
    dmg = draw_damage(rng)
    if protection[target] > 0:
        protection[target] -= 1
        return
    hp[target] = max(0, hp[target] - dmg)


def resolve_card(rng, card, actor, hp, positions, protection, boss_hp, minions):
    ctype, bonus = card
    if ctype == "Melee":
        positions[actor] = "melee"
        if minions > 0:
            minions -= 1
        else:
            boss_hp = max(0, boss_hp - (BASE_WEAPON_DAMAGE + bonus))
    elif ctype == "Ranged":
        positions[actor] = "range"
        if minions > 0:
            minions -= 1
        else:
            boss_hp = max(0, boss_hp - (BASE_WEAPON_DAMAGE + bonus))
    elif ctype == "Healing":
        alive = [i for i in range(len(hp)) if hp[i] > 0]
        if alive:
            target = min(alive, key=lambda i: hp[i])
            hp[target] = min(PLAYER_HP, hp[target] + HEAL_AMOUNT)
    elif ctype == "Protection":
        protection[actor] += 1
    return boss_hp, minions


def simulate_round(rng, n_players, hp, positions, minion_count):
    boss_hp = rng.choice(BOSS_HP_POOL)
    minions = minion_count
    protection = [0] * n_players

    hands = [
        [random.choice(EQUIPMENT_DECK) for _ in range(EQUIP_DRAW[n_players])]
        for _ in range(n_players)
    ]
    hand_ptr = [0] * n_players

    cycles = EQUIP_DRAW[n_players]
    for _ in range(cycles):
        if boss_hp <= 0 and minions <= 0:
            return True
        if all(h <= 0 for h in hp):
            return False

        crab_attack(rng, hp, positions, protection, boss_hp > 0, minions)
        if all(h <= 0 for h in hp):
            return False

        for p in range(n_players):
            if boss_hp <= 0 and minions <= 0:
                break
            if hp[p] <= 0:
                continue
            if hand_ptr[p] >= len(hands[p]):
                continue
            card = hands[p][hand_ptr[p]]
            hand_ptr[p] += 1
            if card[0] == "Protection":
                protection[p] += 1
                continue
            boss_hp, minions = resolve_card(
                rng, card, p, hp, positions, protection, boss_hp, minions
            )

    if boss_hp <= 0 and minions <= 0:
        return True

    # End Phase Combat: each alive player draws+resolves one card, then all
    # remaining crabs attack, repeat until round resolves.
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
            boss_hp, minions = resolve_card(
                rng, card, p, hp, positions, protection, boss_hp, minions
            )
        if boss_hp <= 0 and minions <= 0:
            return True
        if all(h <= 0 for h in hp):
            return False
        alive_crabs = (["boss"] if boss_hp > 0 else []) + ["minion"] * minions
        for _ in alive_crabs:
            if all(h <= 0 for h in hp):
                return False
            crab_attack(rng, hp, positions, protection, boss_hp > 0, minions)
    return boss_hp <= 0 and minions <= 0  # exceedingly rare stalemate fallback


def simulate_game(rng, n_players, minion_count):
    hp = [PLAYER_HP] * n_players
    positions = ["range"] * n_players
    for _ in range(ROUNDS_PER_GAME):
        won_round = simulate_round(rng, n_players, hp, positions, minion_count)
        if not won_round:
            return False
        if all(h <= 0 for h in hp):
            return False
    return True


def win_rate(n_players, minion_count, trials, seed):
    rng = random.Random(seed)
    wins = sum(simulate_game(rng, n_players, minion_count) for _ in range(trials))
    return wins / trials


def find_target_minion_count(n_players, target=0.70, coarse_trials=4000, fine_trials=30000):
    results = {}
    for m in range(1, 9):
        results[m] = win_rate(n_players, m, coarse_trials, seed=1000 * n_players + m)
    # refine the two closest candidates with more trials
    ranked = sorted(results, key=lambda m: abs(results[m] - target))
    best_candidates = ranked[:3]
    refined = {}
    for m in best_candidates:
        refined[m] = win_rate(n_players, m, fine_trials, seed=5000 * n_players + m)
    best_m = min(refined, key=lambda m: abs(refined[m] - target))
    return best_m, refined[best_m], results, refined


if __name__ == "__main__":
    print(f"{'Players':>7} | {'MinionCount':>11} | {'WinRate':>8}  (target 70%)")
    print("-" * 40)
    summary = {}
    for n in (2, 3, 4, 5):
        best_m, best_wr, coarse, refined = find_target_minion_count(n)
        summary[n] = (best_m, best_wr)
        print(f"{n:>7} | {best_m:>11} | {best_wr*100:>7.1f}%")
        detail = ", ".join(f"{m}:{coarse[m]*100:.0f}%" for m in sorted(coarse))
        print(f"        coarse sweep -> {detail}")

    print()
    print("Recommended MinionCount row for locations.csv:")
    print(",".join(str(summary[n][0]) for n in (2, 3, 4, 5)))
