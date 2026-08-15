# Gamma-931 — Character & Boss Roster (Working Draft)

Status: DRAFT. Names are placeholders; most active abilities are still TBD. See `RULES.md`
for the underlying mechanics these abilities plug into.

---

## Characters (8)

| Character | Role | Passive (always on) | Active (limited-use, scales with difficulty) |
|---|---|---|---|
| **Brawler** | Melee | Cleave — melee weapons hit multiple crabs, scaled by remaining HP (formula TBD) | TBD |
| **Biologist** | Support | **Debuffs** the current boss's biome bonus (reduces it, does not cancel it) | TBD |
| **Medic** | Support | Heals for **+1 HP** whenever a heal card is played (by anyone) | **Full team heal**: every player heals 3 HP flat (half of 6 max), capped at max HP |
| **Engineer** | Utility | Can recycle/reuse a spent equipment card once per round | TBD |
| **Scout** | Positioning | Can swap position (melee↔range) once per combat cycle for free | TBD |
| **Marksman** | Ranged | Ranged attacks deal +1 bonus damage (on top of ammo's own bonus) | TBD |
| **Technician** | Utility | Can peek at the next crab action card before it resolves | TBD |
| **Captain** | Leadership | **Team-wide**: when the Captain is first player, *every* player draws one extra equipment card that round (not just the Captain) | TBD |

Notes:
- Medic's passive and active are the only fully-specified abilities so far — use this pairing
  as the template/precedent for fleshing out the rest (a small always-on trickle effect +
  a strong once-in-a-while payoff).
- Biologist's debuff magnitude (how much it reduces the boss bonus by) is still an open
  numbers-balance question, same bucket as Brawler's cleave formula.

---

## Bosses (10)

Abilities below are concrete and numbered — tuned via Monte Carlo simulation
(`tools/boss_ability_simulation.py`) so that drawing any one boss is roughly as
threatening as drawing any other, rather than picked by feel. See
`BALANCE_NOTES.md`'s "Boss Ability Balance Pass" section for methodology,
assumptions, and per-boss results. `AbilityText` in `crab_bosses.csv` has the
authoritative wording; this table is a summary.

### Universal (any location) — 3
| Boss | Concept | Ability |
|---|---|---|
| **Alpha Drone** | Straightforward baseline fight — no gimmick, good early/tutorial boss | None — the control boss every other boss is balanced against |
| **Broodmother** | Spawns an extra minion each round (breeds) | 15% chance/round to spawn 1 extra minion at the start of the fight |
| **Ironshell** | High resistance/armor — needs sustained hits to bring down | None beyond its 8 HP (vs. the usual 6) |

### Biome-specific — 7
Each biome-specific boss's ability is only active when it's drawn at its matching-biome
location (RULES.md's Biologist passive debuffs exactly this "biome bonus"); everywhere
else it fights as a plain 6 HP boss with no gimmick.

| Boss | Biome | Concept | Ability (when biome-active) |
|---|---|---|---|
| **Sandreaver** | Desert | Burrows — buffs minions' range-position targeting priority | Minions: 30% chance of +1 bonus damage vs. a range-position target |
| **Bogfather** | Swamp | Passively heals each round it's in the swamp | Heals 1 HP once, right before End Phase Combat, if still alive |
| **Frostclaw** | Ice/Tundra | Slows players — reduces equipment draw or freezes a card in hand | 40% chance/round to freeze one random player's hand by 1 card (min. 1) |
| **Vinewarden** | Jungle | Regenerates HP tied to its jungle terrain bonus | Regens 1 HP/cycle for the first 2 cycles it survives each round (max +2) |
| **Magmapincer** | Volcanic/Caves | Deals burn damage over time to whoever last hit it | 30% chance/round: 1 unblockable burn to the first player who damages it |
| **Wreckstalker** | Ruins/Wreckage | Ambush — shifts crab targeting priority in its favor | Each of its own attacks: 25% chance to target the first player directly |
| **Tideshell** | Coastal/Reef | Splits into a minion copy when it takes heavy damage | First tier-2 hit/round: 40% chance to spawn 1 extra minion |

Note: 7 biomes are listed for deck depth/variety across playthroughs even though only 5
locations appear in a given game (see `RULES.md` — location count is fixed at 5).

---

## Remaining Design Work

- Draft active abilities for Brawler, Biologist, Engineer, Scout, Marksman, Technician,
  Captain.
- Numbers-balance pass: Brawler cleave formula, Biologist debuff magnitude, active-ability
  use-count-per-difficulty-level table.
- ~~Full ability text for all 10 bosses~~ — done via simulation, see `crab_bosses.csv` and
  `BALANCE_NOTES.md`. ~~Still needs wiring into `RoundEngine.cs`~~ — all 10 are wired up
  and covered by `tests/Gamma931.Core.Tests/BossAbilityTests.cs`. Still needs validation
  against Biologist's debuff once that's designed, since Biologist explicitly reduces
  these biome bonuses.
