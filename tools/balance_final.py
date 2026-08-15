import random
from balance_simulation import simulate_round, PLAYER_HP

def simulate_game_pool(rng, n_players, pool_values):
    """pool_values: list of 7 minion counts, one per non-shuttle location.
    Each game draws 5 of the 7 at random (per RULES.md setup step 2)."""
    chosen = rng.sample(pool_values, 5)
    hp = [PLAYER_HP] * n_players
    positions = ["range"] * n_players
    for minion_count in chosen:
        won_round = simulate_round(rng, n_players, hp, positions, minion_count)
        if not won_round or all(h <= 0 for h in hp):
            return False
    return True

def win_rate_pool(n_players, pool_values, trials, seed):
    rng = random.Random(seed)
    wins = sum(simulate_game_pool(rng, n_players, pool_values) for _ in range(trials))
    return wins / trials

def best_split(n_players, low, high, trials=30000):
    best = None
    for H in range(8):  # 0..7 locations at `high`
        pool = [high] * H + [low] * (7 - H)
        wr = win_rate_pool(n_players, pool, trials, seed=n_players * 1000 + H)
        diff = abs(wr - 0.70)
        print(f"  {n_players}p pool: {H}x{high} + {7-H}x{low}  -> win rate {wr*100:5.1f}% (diff {diff*100:4.1f})")
        if best is None or diff < best[0]:
            best = (diff, H, wr)
    return best

print("3p (low=4, high=5):")
b3 = best_split(3, 4, 5)
print(f"  BEST: H={b3[1]} -> {b3[2]*100:.1f}%\n")

print("4p (low=7, high=8):")
b4 = best_split(4, 7, 8)
print(f"  BEST: H={b4[1]} -> {b4[2]*100:.1f}%\n")

print("5p (low=7, high=8):")
b5 = best_split(5, 7, 8)
print(f"  BEST: H={b5[1]} -> {b5[2]*100:.1f}%\n")

print("2p (flat 1, rules-mandated floor):")
wr2 = win_rate_pool(2, [1]*7, 30000, seed=1)
print(f"  flat 1 -> {wr2*100:.1f}% (target 70% unreachable via minion count alone at 2p)")
