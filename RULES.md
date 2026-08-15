# Gamma-931 — Rules (Working Draft)

Status: DRAFT — contains open design questions, marked `[OPEN]`. Not final.

## Decisions Log
- Damage model: **shared 6HP pool** (not per-limb). Body location just sets hit cost.
- Combat cycle **loops**: draw crab action → all players play equipment → repeat, until all
  crabs or all players are dead, or equipment runs out (then End Phase).
- Melee/range **position is determined by equipment played** (playing a melee weapon puts you
  in melee position, a ranged weapon puts you in range position) — dynamic, not fixed per
  character or chosen as a formation.
- **Weapons are not drawn**. Every player always has a default melee weapon and a default
  ranged weapon, each dealing a flat 1 HP hit on their own. Instead of single-use weapon cards,
  the equipment deck's Melee/Ranged cards are **weapon modifiers** (melee) or **ammo** (ranged):
  playing one attacks with the matching default weapon for 1 HP **plus** that card's bonus, then
  the card is discarded (single-use boost, not a permanent attachment). See "Weapons &
  Modifiers" below.
- **A player's turn plays one equipment card first; only a weapon modifier/ammo card also lets
  them attack.** Playing a Healing, Protection, or Utility card is the player's whole turn — no
  attack that cycle. There is no way to attack without playing a matching modifier/ammo card
  from hand (same as the old "no weapon card in hand → no attack" constraint).
- Crab **complexity lives only on bosses**. Minions stay flat, simple HP creatures with no
  special traits (no resistances/healing/splitting on minions).
- Position **persists** once set (playing a melee weapon keeps you in melee position until
  you play a ranged weapon, and vice versa) — it does not reset each round or each cycle.
- Players **default to range position** at the start of a round, before playing any equipment.
- Minion crabs are a **flat 1 HP** — any hit kills one. All real threat comes from bosses.
- Deck exhaustion (equipment, damage, minion, or any other deck): **reshuffle the discard pile**
  into a new deck and continue play uninterrupted. In particular, once the equipment pile or the
  minion pile is drawn empty, its own discard pile (played equipment cards; killed minion cards)
  is reshuffled into a new draw pile for that deck — each deck's discard only ever feeds back
  into itself, decks are never merged.
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

**`[NEW]` Playing a card is always the turn action; only a weapon modifier/ammo card also
attacks.** A player plays exactly one equipment card on their turn, same as before. If it's a
weapon modifier (Melee) or ammo (Ranged), playing it also attacks with the matching default
weapon (see "Weapons & Modifiers" above) — that's the whole point of playing it. If it's a
Healing, Protection, or Utility card, that card's own effect is the whole turn; no attack
happens that cycle.

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

## Weapons & Modifiers `[NEW]`

There is no separate "weapon deck" and weapons are never drawn or run out on their own:

- Every player always has a **default melee weapon** and a **default ranged weapon**, each
  dealing a flat **1 HP** hit by itself.
- The equipment deck's Melee cards are **weapon modifiers**; its Ranged cards are **ammo**.
  Playing one attacks with the matching default weapon for 1 HP plus that card's bonus (e.g. a
  +1 modifier makes a melee attack deal 2 HP total), then the card is discarded — a single-use
  boost, not a permanent attachment to the weapon.
- Playing a modifier/ammo card is what lets a player attack that combat cycle (see Combat Turn
  Order below) — it also sets their position (melee/range), same as the old single-use weapon
  cards did.
- A minion still dies to any hit regardless of its size, so a boosted attack can kill more than
  one minion in the same hit (up to the total HP dealt), matching how weapon tiers worked before.

---

## End Phase Combat

Reached once all players have played all their equipment for the round (their hands are
empty) but crabs remain.

1. Players and crabs fight in sudden death — no new crabs enter play.
2. Starting with the first player, each player either:
   - draws and reveals the top card of the equipment deck and resolves its effects immediately
     (same as before — a Melee/Ranged draw attacks with the bonus per "Weapons & Modifiers", a
     Healing/Protection/Utility draw does not), **or**
   - `[NEW]` skips the draw and attacks with their **default melee weapon** for its flat 1 HP,
     guaranteeing at least some offense in sudden death instead of relying on a random draw.

   Continue until all players have acted once.
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

**Confirmed**: this damage-deck draw is the same for every attacking crab, boss or minion
— there is no per-crab attack stat. Minion crab **types are flavor/cosmetic only**: they
carry no passive or active abilities and deal identical 1-2 HP damage (per the shared damage
deck) regardless of which minion card is in play. All mechanical complexity (resistances,
healing, splitting, unique attacks) lives on boss cards only, per the "crab complexity lives
only on bosses" decision above.

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
