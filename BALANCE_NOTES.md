# Minion Count Balance Pass — Monte Carlo Simulation

Status: DRAFT, same as RULES.md/ROSTER.md — produced by simulation against a
baseline ruleset, not real playtesting. Superseded the moment real playtests
disagree with it.

## Goal

RULES.md leaves per-location minion counts as an open numbers-balance question
("all per-location/per-player-count values need real numbers... expected to
happen through playtesting once the app exists"). Absent real playtesting data,
this pass uses a Monte Carlo combat simulator (`tools/balance_simulation.py`,
`tools/balance_final.py`) to solve for minion counts that produce a **~70%
full-campaign (5-location) win rate** at each player count (2–5), using only
the numeric rules RULES.md has already confirmed.

## What's confirmed vs. assumed

**Confirmed (taken directly from RULES.md / the CSVs, unchanged):**
- 6 HP per player; damage-deck distribution (24/28 cards deal 1 dmg, 4/28 deal 2)
- Minion crabs: flat 1 HP, any hit kills
- Boss HP: 6 for 9 of 10 bosses, 8 for Ironshell — drawn from a single shared deck
- Equipment deck composition and damage tiers (14 cards: 6 offensive, 2
  protection, 2 healing, 4 utility)
- `EquipmentDrawXp` and `CrabActionCountXp` per player count (unchanged)
- 5 of the 7 non-shuttle locations are drawn per game (RULES.md setup step 2)
- Minimum of 1 minion per location (RULES.md Round Structure step 5)

**Assumed, because RULES.md marks these `TBD`/placeholder and a simulation
needs *something* concrete to run against:**
1. No character passives/actives are modeled — this is a generic-party
   baseline. Real characters (Medic healing, Brawler cleave, Captain's extra
   draws, etc.) can only make the game *easier*, so tuning against this floor
   keeps the campaign beatable even before character balance is finalized.
2. Each crab-action-card draw resolves as one attack from one random alive
   crab against the priority target (melee > range > first player) — all 6
   `crab_actions.csv` effects are literal placeholders, so this is the
   simplest reading consistent with the Crab Attack Rules section.
3. Equipment targeting: players always finish off minions before damaging
   the boss (minions die to any hit, so clearing them first is optimal play).
4. Protection cards are held and auto-block the next attack on that player
   (rational play, matches "hold and play as a response").
5. Healing cards heal the most-injured living player 2 HP flat (no Medic
   bonus, per assumption 1).
6. Utility cards have no modeled combat effect (their effect text is TBD).
7. Equipment hands are sampled i.i.d. from the 14-card deck's proportions
   rather than tracked as an exact shared shoe — reasonable given RULES.md's
   own reshuffle-on-exhaustion rule.

These are documented in `tools/balance_simulation.py` and should be revisited
once the real crab action / character ability text is written — at that point
this simulation should be re-run rather than trusted as-is.

## Method

1. `tools/balance_simulation.py` simulates one full 5-round campaign per
   player count and reports win/loss, then coarse-sweeps minion count 1–8.
2. `tools/balance_final.py` refines this against the *actual* setup procedure
   (5 of 7 locations drawn at random each game, not a fixed round order),
   sweeping how many of the 7 non-shuttle locations get a "high" minion value
   vs. a "low" one, to land closer to 70% than a single flat integer can.
3. Each configuration was run at 20,000–30,000 trials per data point.

## Results

| Players | Result | Configuration |
|---|---|---|
| 2p | **19.8%** (target unreachable via minion count) | flat 1 minion/location (rules-mandated floor) |
| 3p | **71.1%** | 1 of 7 locations at 5 minions, other 6 at 4 |
| 4p | **70.1%** | 3 of 7 locations at 8 minions, other 4 at 7 |
| 5p | **69.3%** | 2 of 7 locations at 9 minions, other 5 at 8 |

3p/4p/5p all land within ~1pp of the 70% target. `Data/locations.csv` has
been updated with these values (high-value locations assigned to `LOC-01`
through `LOC-0N` for each column, arbitrarily but consistently — swap which
specific locations carry the high value freely, only the count matters for
the target win rate).

### 2p is a real finding, not a rounding error

At 2 players, minion count *cannot* reach 70% — RULES.md's own floor of 1
minion/location already produces only a ~20% simulated win rate, and adding
more minions only makes it worse. Diagnostics in `tools/balance_diagnostics.py`
show:
- More `EquipmentDraw2p` makes 2p **worse**, not better (more combat cycles
  mean more crab attacks landing on a pool of just 2 players before the
  boss goes down — cycle count doesn't have anywhere near the leverage to
  compensate for the concentration of incoming attacks).
- Boss HP is the dominant lever: even cutting boss HP roughly in half (to
  ~3, +2 for Ironshell) only brings 2p to ~41% — still short of 70%.

**Recommendation:** 2p needs a fix outside this task's scope (minion count) —
most likely per-player-count boss HP scaling, or character passives/actives
being required (rather than optional) at 2p. Flagging this for a follow-up
pass rather than forcing an artificial minion number that this simulation
shows can't actually deliver 70%.

## Physical card consequence

The highest simulated minion requirement is **9** (both 5p "high" locations).
`crab_minions.csv` previously had only 5 distinct minion cards — not enough
to draw 9 simultaneously without repeating a physical card mid-draw. Added
4 new minion types (`MIN-06`–`MIN-09`: Claw Scrapper, Barnacle Biter, Husk
Crawler, Tide Snapper) to cover the max simultaneous draw.

`Data/physical_card_assignments.csv` was regenerated accordingly: 83 of the
104 physical cards (two standard decks) are now assigned (was 79), leaving
21 reserved for future content (was 25).
