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

## Results (original pass, superseded — see "Weapon Rework Re-Tune" below)

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

**These numbers are stale.** They were produced against the pre-"Weapons &
Modifiers" flat-tier weapon model. See "Weapon Rework Re-Tune" below for the
current `locations.csv` values and the re-run boss ability pass.

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

**Update, weapon rework**: the single-use Flare Gun (`EQ-06`) was removed from
`equipment.csv` (RULES.md "Weapons & Modifiers" — it was a Ranged weapon whose only
differentiator, "distracts crabs," was unimplemented flavor text with no mechanical
effect). Its physical card slot (`8C-A`) reverts to `Unused (Reserved)`: 82 of 104
assigned, 22 reserved.

---

## Weapon Rework Re-Tune

RULES.md's weapon model changed from single-use weapon cards (their own flat damage
tier) to a default weapon (flat `BaseWeaponDamage` = 1) boosted by a played
modifier/ammo card's `DamageBonus` — a net damage buff, since every former tier-1
weapon hit (1 dmg) now needs a modifier card to attack at all but every card that does
attack deals at least 2 (1 base + >= 1 bonus). `tools/balance_simulation.py` and
`tools/boss_ability_simulation.py` were updated to model this (`BASE_WEAPON_DAMAGE`,
`EQUIPMENT_DECK` bonus values, `EQ-06` Flare Gun removed) and both re-run in full.

### Minion count re-tune

The Alpha Drone control baseline (no ability, at the previous reference minion counts)
confirms the buff: 2p jumped from ~20% to **~40%**, 3p/4p/5p all rose several points
too. Re-running the same coarse-sweep-then-location-split method as the original pass
against these higher baselines:

| Players | Result | Configuration |
|---|---|---|
| 2p | **39.8%** (target still unreachable via minion count) | flat 1 minion/location (rules-mandated floor, unchanged) |
| 3p | **68.0%** | 2 of 7 locations at 5 minions, other 5 at 4 |
| 4p | **70.7%** | flat 7 minions at all 7 locations (no split needed) |
| 5p | **71.5%** | 5 of 7 locations at 8 minions, other 2 at 7 |

3p/4p/5p land within ~2pp of the 70% target (slightly looser than the original ~1pp,
an artifact of only 8 possible split ratios across 7 locations combined with the
steeper win-rate cliff the damage buff creates between adjacent minion counts — see
the coarse sweeps in `tools/balance_simulation.py`'s own output). `Data/locations.csv`
has been updated with these values. Note the 5p split **flipped which value is the
majority** (8 is now the common case, 7 the exception) compared to the original pass,
where 8 was the low/exception value — worth remembering if a future pass tunes new
locations, since "majority = easy" is no longer a safe assumption to carry over.

**2p is still unfixed**, and worse relative to the target than before in absolute
terms (39.8% vs. a 70% target is still a ~30pp gap, versus the original pass's ~50pp
gap) but the underlying diagnosis in `tools/balance_diagnostics.py` (more equipment
draws make 2p *worse*, not better; boss HP is the dominant lever) hasn't been
re-verified against the new damage model. Same recommendation as before: 2p needs a
fix outside minion count — most likely per-player-count boss HP scaling or making
character passives/actives mandatory rather than optional at 2p.

### Boss ability magnitudes: unchanged

Re-running `tools/boss_ability_simulation.py`'s full sweep (same reference minion
counts by coincidence — the re-tuned "majority" value per player count above happens
to match `REF_MINIONS` exactly: 2p=1, 3p=4, 4p=7, 5p=8) picked the **same best
parameter for all 8 tunable bosses** as the original pass: Broodmother 0.15,
Bogfather 1 HP, Frostclaw 0.40, Vinewarden (1 HP/tick, cap 2), Magmapincer 0.30,
Sandreaver 0.30, Wreckstalker 0.25, Tideshell (tier 2, +1 minion, 0.40). No changes
needed in `RoundEngine.cs` or `crab_bosses.csv` — the existing "Results (isolated,
boss drawn every round)" table above is still current. This makes sense: each
ability's candidate values were already spaced widely enough (e.g. Broodmother's
0.15/0.25/0.35/0.5) that the damage buff shifting the underlying baseline didn't
change which candidate sits closest to it.

---

# Boss Ability Balance Pass

## Goal

`crab_bosses.csv`'s `AbilityText` was `TBD` for all 10 bosses — ROSTER.md only had
one-line concepts ("spawns an extra minion each round," "passively heals," etc.), no
numbers. This pass turns each concept into a concrete, numbered ability and tunes its
magnitude so that **drawing any one boss is roughly as threatening as drawing any
other** — parity across the shared 10-card boss deck, rather than a single global
win-rate target (that's what the minion pass solved; this pass solves for boss-to-boss
fairness on top of it).

## Method

`tools/boss_ability_simulation.py` extends the minion-pass combat model
(`tools/balance_simulation.py`) with per-boss ability hooks, then:
1. Establishes Alpha Drone (no ability) as the control baseline per player count, at a
   fixed reference minion count (the tuned locations.csv "low"/majority value: 2p=1,
   3p=4, 4p=7, 5p=8).
2. For each other boss, sweeps its ability's magnitude with that boss drawn **every
   round** (an isolated worst-case stress test — see the biome-gating note below) and
   picks the value whose win rate deviates least from Alpha Drone's, averaged across
   3p/4p/5p (2p excluded — already floor-locked per the minion pass, see above).

**First-pass finding:** "every single trigger" designs are catastrophically strong.
A flat +1 minion every round Broodmother is drawn cost ~26 percentage points of win
rate on average — consistent with how steep the minion-count curve already is near the
70% target (see the minion-count sweep tables above, where +1 minion routinely swings
win rate by 15-30pp). Guaranteed-trigger burn damage, party-wide hand reduction, and
uncapped per-cycle regen were all similarly overtuned even at their smallest tested
magnitude. **Every ability below was redesigned around a probability or a cap** rather
than a guaranteed trigger, then re-tuned — this is the same "buff, don't overload"
restraint RULES.md's Character Design section already applies to players, extended to
bosses.

## Results (isolated, boss drawn every round)

| Boss | Ability | 3p Δ | 4p Δ | 5p Δ | avg |Δ| |
|---|---|---|---|---|---|
| Alpha Drone | none (control) | — | — | — | — |
| Ironshell | 8 HP only, no ability | -6.9 | -1.4 | -0.1 | (reference) |
| Vinewarden | regen 1 HP/cycle, first 2 cycles/round (cap +2) | -0.1 | +0.1 | -0.1 | **0.1** |
| Bogfather | heal 1 HP once, before End Phase | -0.9 | -0.1 | -0.1 | **0.4** |
| Wreckstalker | 25% chance/attack: ambush the first player | +0.7 | +1.5 | +2.9 | **1.7** |
| Sandreaver | minions: 30% chance +1 dmg vs. range position | -3.0 | -1.2 | -1.3 | **1.8** |
| Magmapincer | 30% chance/round: 1 burn to first player to hit it | -4.8 | -2.7 | -1.3 | **2.9** |
| Tideshell | first tier-2 hit/round: 40% chance to spawn 1 minion | -7.2 | -2.5 | -0.8 | **3.5** |
| Broodmother | 15% chance/round: +1 extra minion | -6.5 | -6.6 | -7.4 | **4.3** |
| Frostclaw | 40% chance/round: freeze 1 random player's hand -1 card | -4.0 | -4.3 | -4.6 | **4.3** |

All 8 tunable abilities land within ~5pp of Alpha Drone's baseline — Ironshell (no
ability, just +2 HP) is left as a natural reference point rather than force-matched,
since "a beefier standard fight" is its whole concept.

## Biome-gating means this is a deliberate worst case

Per `CrabBossCard.IsActiveAt()` in the C# model, the 7 biome-specific bosses' abilities
are only active when the drawn boss's biome matches the current location's biome —
everywhere else they fight as a plain 6 HP boss, identical to Alpha Drone. This pass
tuned magnitudes assuming the ability is **always** active (boss drawn every round),
which is a deliberately pessimistic stress test: in real play, a biome-specific boss's
ability only fires on a biome match, which happens well under half the time it's drawn
(at most 1 of the 5 locations in a given game matches its biome, and it's competing
against 9 other bosses in the shared draw). Real average difficulty is therefore softer
than the table above — intentional headroom, not a gap to close.

## Wired into the engine

All 10 abilities are implemented in `RoundEngine.cs`, using the magnitudes from the
table above, matching biome-gating via `CrabBossCard.IsActiveAt()`:
- **Broodmother** — `DrawMinions` rolls the extra-minion chance.
- **Sandreaver** — `ResolveCrabAttack` rolls the range-position bonus damage when the
  attacker is a minion (RoundEngine now determines boss-vs-minion attacker identity for
  every crab attack, main-cycle included, via `SelectCrabAttacker`, not just End Phase).
- **Bogfather** — `BeginEndPhaseCombat` applies the once-per-round heal, latched by
  `GameState.BossAbilityUsedThisRound` (that method re-runs every End Phase iteration).
- **Frostclaw** — `DrawBoss` rolls the freeze and removes a card from a random player's
  already-drawn hand (equipment is drawn *before* the boss is revealed per RULES.md, so
  the ability acts on the hand after the fact rather than the draw count).
- **Vinewarden** — `TransitionWhenNoOneCanPlay` applies the regen tick at the true end of
  each combat cycle (after that cycle's crab attack and equipment turns, matching
  `tools/boss_ability_simulation.py`'s `end_of_cycle_regen()`), capped via
  `GameState.BossAbilityTicksThisRound`.
- **Magmapincer** / **Tideshell** — `ResolveWeaponHit`'s boss-hit branch: each rolls once,
  latched by `BossAbilityUsedThisRound`, on the first hit that lands (Tideshell only
  counts tier-2+ hits).
- **Wreckstalker** — `SelectAttackTargetWithAmbush` rolls the ambush before falling back
  to the normal melee/range/first-player priority, used by both the main cycle and End
  Phase Combat.
- **Ironshell** / **Alpha Drone** — no code beyond `StartingHp`, already loaded from CSV.

Covered by `tests/Gamma931.Core.Tests/BossAbilityTests.cs`, which forces each ability's
probability roll deterministically (a `Random` subclass overriding `NextDouble()`) rather
than hunting for a lucky seed.

**Still open:** Biologist's passive explicitly *debuffs* "the current boss's biome
bonus" — once Biologist's debuff magnitude is designed, re-run this pass with it applied,
since it directly interacts with every biome-specific ability above. Character active
abilities besides Medic's remain `TBD` and unwired, same as before this pass.
