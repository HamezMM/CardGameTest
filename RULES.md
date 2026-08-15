# Gamma-931 — Rules (Working Draft)

Status: DRAFT — contains open design questions, marked `[OPEN]`. Not final.

## Decisions Log
- Damage model: **shared 6HP pool** (not per-limb). Body location just sets hit cost.
- Combat cycle **loops**: draw crab action → all players play equipment → repeat, until all
  crabs or all players are dead, or equipment runs out (then End Phase).
- Melee/range **position is determined by equipment played** (playing a melee weapon puts you
  in melee position, a ranged weapon puts you in range position) — dynamic, not fixed per
  character or chosen as a formation.
- Crab **complexity lives only on bosses**. Minions stay flat, simple HP creatures with no
  special traits (no resistances/healing/splitting on minions).
- Position **persists** once set (playing a melee weapon keeps you in melee position until
  you play a ranged weapon, and vice versa) — it does not reset each round or each cycle.
- Players **default to range position** at the start of a round, before playing any equipment.
- Minion crabs are a **flat 1 HP** — any hit kills one. All real threat comes from bosses.
- Deck exhaustion (equipment, damage, or crab deck): **reshuffle the discard pile** into a new
  deck and continue play uninterrupted.
- **Cleave is not a general melee rule** — normal melee equipment hits one crab. Cleave is a
  Brawler-archetype character passive: any melee weapon that character plays triggers cleave,
  hitting a number of crabs based on their *remaining* HP (exact scaling formula TBD).
- Location count is **always 5** (non-shuttle) regardless of player count. Difficulty scales
  instead via **boss minion count scaling with player count** (more players → more minions
  spawned with the boss), plus the per-player-count numbers already on location cards.
- **Loss condition**: all players dead = game over. No secondary loss condition.
- **First player rotates** each round (passes clockwise to the next location), sharing both
  the equipment-order advantage and the fallback crab-targeting risk around the table.
- **No loot on kill**. Equipment comes purely from the per-location draw count, not combat
  drops — keeps fights fast with no mid-combat bookkeeping.
- **Initial content scope for first playtest build**: 6-8 character archetypes; ~8-10 bosses
  total as a mixed pool of universal and biome-specific.

## Premise
A co-op roguelike card game for 2-5 players. Your crew has crash-landed and must fight
through 5 alien locations back to the shuttle. The game is won when the shuttle location
is revealed — there is no combat there.

---

## Setup

1. Each player chooses a character; set the remaining character cards aside.
2. Set the shuttle location face up in play. Choose 5 other location cards, shuffle, and
   place face down on top of the shuttle location — this is the location deck.
3. Shuffle the following into their own separate face-down decks: equipment, damage,
   crab boss, crab action, crab (minion).
4. **Confirmed**: the location deck is always exactly 5 non-shuttle locations, regardless of
   player count. Difficulty instead scales via boss minion count (see Round Structure) and the
   per-player-count numbers printed on each location card.

---

## Round Structure

1. Reveal the top card of the location deck; leave it face up on top of the deck. It shows:
   - Number of equipment cards each player draws
   - Number of crab action cards set aside for the round
   - `[NEW]` These numbers vary by player count (2/3/4/5) printed on the same card, so one
     location deck serves all player counts — no separate card sets needed.
2. Each player draws the shown number of equipment cards.
3. Draw the top card of the **crab boss deck** — single shared deck, not per-biome.
   - Boss has abilities/bonuses. Some bosses are universal (work at any location), some are
     biome-specific (only relevant/active at matching location types).
   - Drawing the boss *after* the location card keeps setup simple (no need to pre-sort a
     boss deck per biome).
   - Scope for first playtest build: **10 total boss cards** (3 universal, 7 biome-specific),
     drafted in `ROSTER.md`. `[OPEN]` Full ability text still needs to be designed.
4. Set aside the shown number of crab action cards, face down — drawn down through combat.
5. Draw the top card of the crab deck and put it in play as the boss's minion(s).
   - `[NEW]` Minimum of 1 crab minion per location so every location can support a boss draw.
   - Every additional minion crab added at that location (e.g. via boss ability) is a **flat
     1 HP** creature with no special abilities — any hit kills one. Only the boss carries
     complexity or extra HP.
   - **Minion count scales with player count**: bosses spawn more minions in larger games,
     keeping the fixed 5-location run appropriately challenging for 2 vs. 5 players.

### Combat Turn Order
Repeats each cycle until the round ends:
1. Draw and resolve a crab action card.
2. First player plays an equipment card.
3. Subsequent players, clockwise from first player, each play an equipment card.
4. **Confirmed**: this whole 4-step cycle repeats (draw new crab action, all players play
   equipment again) until the round-end condition is hit — not just a single pass.

A round ends when either all crabs (boss + minions) or all players are dead.
If crabs remain after all equipment cards are exhausted for the round, move to **End Phase Combat**.
Once the round ends, remove the revealed location card and reveal the next one.

### Melee Cleave
Normal melee equipment hits one crab. Cleave is **not** a general rule — it is a Brawler
archetype character passive (see Character Design): when the Brawler plays any melee weapon,
it triggers cleave, hitting a number of crabs based on the Brawler's remaining HP.
`[OPEN]` Exact scaling formula (e.g. 1 crab per N HP remaining) is a numbers-balance detail
still to be tuned via playtesting.

---

## End Phase Combat

1. Players and crabs fight in sudden death — no new crabs enter play.
2. Starting with the first player, draw and reveal the top card of the equipment deck and
   resolve its effects immediately. Continue until all players have drawn once.
3. All remaining crabs attack.
4. Repeat until the round is over (all crabs or all players dead).

---

## Crab Attack Rules

Crabs target players in this priority order:
1. Any player in melee attack position
2. Any player in range position
3. The round's first player

**Confirmed**: players enter melee/range position dynamically by the equipment they play (a
melee weapon card puts you in melee position, a ranged weapon card puts you in range
position). Position persists until overridden by playing the other weapon type. Players
default to **range position** at the start of a round, before playing any equipment.

For each crab attack, draw one card from the damage deck and resolve it.
Crab attacks can be blocked by playing a protection equipment card as a response, out of
turn. Healing cards must be played in normal turn order (not as a response).

---

## Damage & Health

Each character has 6 HP total, tracked as a **single shared pool** (not per-limb). Damage by
body location determines how much a given hit costs against that pool:
- Arms: 1 HP · Head: 2 HP · Legs: 1 HP · Torso: 1 HP
A character dies once they've taken 6 HP of damage.

(Per-limb HP tracking, disable-vs-injure attack stats, and permanent unhealable limb damage
were considered and explicitly not pursued — too much bookkeeping for the "quick roguelike"
goal.)

**Resolved**: minions stay flat, simple HP creatures — no resistances, passive healing, or
split-on-death traits. All such complexity (resistances, healing, splitting, terrain bonuses)
lives on **boss cards only**, matching the "buff players, don't overload enemies with rules"
philosophy and keeping rounds fast.

**HP tracking mechanism** (relevant mainly for the physical product, moot for the digital
playtester since state is just tracked in software): BANG!-style card rotation vs. damage
tokens vs. a card back system.

---

## Character Design `[NEW]`

Design philosophy locked in: **buff players rather than weaken/negate enemies** — a *full
negation* of boss abilities was tried (early Biologist idea) and rejected as OP and as
deflating the excitement of boss fights. A partial **debuff** (reduces, doesn't cancel, a
boss's bonus) was later judged to be the right middle ground and is what the Biologist uses.

Direction:
- Each character has a passive ability (always on).
- Each character has a powerful **limited-use active ability**.
- The number of uses of the active scales with difficulty level (fewer uses = harder game).
- Example: Brawler's passive is cleave — any melee weapon they play hits multiple crabs,
  scaled by their remaining HP (exact formula TBD).

Scope for first playtest build: **8 character archetypes**, drafted in `ROSTER.md`.
`[OPEN]` Active abilities are still TBD for most characters — see `ROSTER.md`.

---

## Winning / Losing

- **Win**: the game is won once the shuttle location is revealed. No combat occurs there.
- **Loss**: all players dead. No secondary loss condition.

---

## Remaining Design Work

- Full character roster (6-8 archetypes) with passive + limited-use active abilities per
  character, and difficulty-based scaling for active-ability use counts.
- Full boss roster (~8-10 bosses) with abilities/terrain bonuses, universal vs. biome-specific
  split.
- Exact numbers/tuning: equipment draw counts, crab action counts, minion count per player
  count, Brawler cleave formula — all per-location/per-player-count values need real numbers
  before the location and boss card sets can be built. This is expected to happen through
  playtesting once the app exists.
