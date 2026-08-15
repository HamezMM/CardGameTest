import random
from balance_simulation import (
    simulate_game, win_rate, EQUIP_DRAW, simulate_round, PLAYER_HP,
)
import balance_simulation as bs

# 1. Is minion_count=0 still under 70% for 2p? (checks whether the shortfall is
#    structural -- i.e. minion count isn't the lever that can fix 2p at all)
print("2p, minion_count=0:", win_rate(2, 0, 30000, seed=1) * 100, "%")
print("2p, minion_count=1:", win_rate(2, 1, 30000, seed=2) * 100, "%")

# 2. What EquipmentDraw2p (currently 4) would 2p need, at the rules-mandated minimum
#    of 1 minion/round, to reach ~70%? Patch EQUIP_DRAW[2] and re-run.
print()
print("2p sensitivity to EquipmentDraw2p, at minion_count=1:")
for draw in (4, 5, 6, 7, 8, 9, 10):
    bs.EQUIP_DRAW[2] = draw
    wr = win_rate(2, 1, 20000, seed=100 + draw)
    print(f"  EquipmentDraw2p={draw:2d} -> win rate {wr*100:5.1f}%")
bs.EQUIP_DRAW[2] = 4  # restore

# 3. Mixed per-location minion schedules for 4p and 5p, to land closer to 70%
#    than the nearest single integer (7 vs 8 for both).
def simulate_game_schedule(rng, n_players, schedule):
    hp = [PLAYER_HP] * n_players
    positions = ["range"] * n_players
    for minion_count in schedule:
        won_round = simulate_round(rng, n_players, hp, positions, minion_count)
        if not won_round or all(h <= 0 for h in hp):
            return False
    return True

def win_rate_schedule(n_players, schedule, trials, seed):
    rng = random.Random(seed)
    wins = sum(simulate_game_schedule(rng, n_players, schedule) for _ in range(trials))
    return wins / trials

print()
print("4p mixed schedules (5 locations, target 70%):")
for k in range(6):  # k locations at 8 minions, (5-k) at 7
    schedule = [8] * k + [7] * (5 - k)
    wr = win_rate_schedule(4, schedule, 25000, seed=300 + k)
    print(f"  {k} loc@8 + {5-k} loc@7 -> {wr*100:5.1f}%")

print()
print("5p mixed schedules (5 locations, target 70%):")
for k in range(6):
    schedule = [8] * k + [7] * (5 - k)
    wr = win_rate_schedule(5, schedule, 25000, seed=400 + k)
    print(f"  {k} loc@8 + {5-k} loc@7 -> {wr*100:5.1f}%")

# 4. Why does more equipment make 2p worse? Check the other direction too, and
#    check sensitivity to boss HP (diagnostic only -- boss HP isn't the lever this
#    task asked to tune, but it explains *why* minion count can't fix 2p alone).
print()
print("2p sensitivity to EquipmentDraw2p (both directions), minion_count=1:")
for draw in (2, 3, 4, 5, 6):
    bs.EQUIP_DRAW[2] = draw
    wr = win_rate(2, 1, 20000, seed=200 + draw)
    print(f"  EquipmentDraw2p={draw:2d} -> win rate {wr*100:5.1f}%")
bs.EQUIP_DRAW[2] = 4

print()
print("2p sensitivity to boss HP pool (draw=4, minion=1):")
orig_pool = bs.BOSS_HP_POOL[:]
for scale_hp in (3, 4, 5, 6):
    bs.BOSS_HP_POOL = [scale_hp] * 9 + [scale_hp + 2]
    wr = win_rate(2, 1, 20000, seed=500 + scale_hp)
    print(f"  BossHP~{scale_hp:d}(+2 Ironshell) -> win rate {wr*100:5.1f}%")
bs.BOSS_HP_POOL = orig_pool

# 5. Refine 3p (4 vs 5) and 5p (8 vs 9) mixed schedules toward exactly 70%.
print()
print("3p mixed schedules (4 vs 5 minions):")
for k in range(6):
    schedule = [5] * k + [4] * (5 - k)
    wr = win_rate_schedule(3, schedule, 25000, seed=600 + k)
    print(f"  {k} loc@5 + {5-k} loc@4 -> {wr*100:5.1f}%")

print()
print("5p mixed schedules (8 vs 9 minions):")
for k in range(6):
    schedule = [9] * k + [8] * (5 - k)
    wr = win_rate_schedule(5, schedule, 25000, seed=700 + k)
    print(f"  {k} loc@9 + {5-k} loc@8 -> {wr*100:5.1f}%")
