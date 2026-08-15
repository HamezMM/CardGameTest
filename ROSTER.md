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
| **Marksman** | Ranged | Ranged weapons deal +1 damage tier | TBD |
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

### Universal (any location) — 3
| Boss | Concept |
|---|---|
| **Alpha Drone** | Straightforward baseline fight — no gimmick, good early/tutorial boss |
| **Broodmother** | Spawns an extra minion each round (breeds) |
| **Ironshell** | High resistance/armor — needs sustained hits to bring down |

### Biome-specific — 7
| Boss | Biome | Concept |
|---|---|---|
| **Sandreaver** | Desert | Burrows — buffs minions' range-position targeting priority |
| **Bogfather** | Swamp | Passively heals each round it's in the swamp |
| **Frostclaw** | Ice/Tundra | Slows players — reduces equipment draw or freezes a card in hand |
| **Vinewarden** | Jungle | Regenerates HP tied to its jungle terrain bonus |
| **Magmapincer** | Volcanic/Caves | Deals burn damage over time to whoever last hit it |
| **Wreckstalker** | Ruins/Wreckage | Ambush — shifts crab targeting priority in its favor |
| **Tideshell** | Coastal/Reef | Splits into a minion copy when it takes heavy damage |

Note: 7 biomes are listed for deck depth/variety across playthroughs even though only 5
locations appear in a given game (see `RULES.md` — location count is fixed at 5).

---

## Remaining Design Work

- Draft active abilities for Brawler, Biologist, Engineer, Scout, Marksman, Technician,
  Captain.
- Numbers-balance pass: Brawler cleave formula, Biologist debuff magnitude, active-ability
  use-count-per-difficulty-level table.
- Full ability text for all 10 bosses (currently one-line concepts only).
