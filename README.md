# Gamma-931 — Digital Playtester

A C# WPF desktop app for playtesting *Gamma-931*, a co-op roguelike card game for 2-5 players.
See [`RULES.md`](RULES.md) and [`ROSTER.md`](ROSTER.md) for the game design this app implements.

## Why CSV

Every card (characters, bosses, minions, damage, equipment, locations) is loaded
from the CSV files in [`/Data`](Data) at app startup — nothing is hardcoded or compiled in. To
tweak a number or add a card, edit the CSV and relaunch the app; no rebuild needed. Several values
(boss HP, weapon damage, heal amounts, per-location draw counts) are placeholders since RULES.md
doesn't pin them down yet — they're marked as such in the CSVs and in code comments, and are meant
to be tuned once real playtesting starts.

## Project layout

```
Gamma931.sln
Data/                        Card data as CSV — edit these to tweak the game
src/
  Gamma931.Core/             Game logic, no UI dependency
    Models/                  Card types (CharacterCard, CrabBossCard, EquipmentCard, ...)
    Data/                    CSV -> CardDatabase loading (CsvCardLoader)
    Game/                    Deck, Player, GameState, RoundEngine (the round/combat state machine)
  Gamma931.App/               WPF UI (net8.0-windows)
    Views/                   CardBrowserView (search/filter all loaded cards), GameView (Play tab)
    ViewModels/               Hand-rolled MVVM (no external framework dependency)
    Controls/                CardFaceView -- the generic card widget every card-shaped surface builds on
    Converters/              CardToImagePathConverter binds any card model straight to its art
    Services/                CardImageCatalog maps a card model to its Assets/Cards/... image path
    Assets/Cards/            Generated card face art (see tools/generate_card_art.py below)
tests/
  Gamma931.Core.Tests/        xUnit tests for CSV loading and the round engine
```

## Card art

Every card gets flat-icon face art, generated procedurally by
[`tools/generate_card_art.py`](tools/generate_card_art.py) (Python + Pillow) straight from the
CSVs in `/Data` -- there's no hand-authored art to keep in sync. Each category gets its own
color palette and icon set (character role, boss biome, equipment type, location biome, ...),
with per-card variety from a deterministic seed on the card's Id, so re-running the script is a
no-op unless `/Data` changed. Output goes to `src/Gamma931.App/Assets/Cards/<category>/<id>.png`
(one PNG per character/boss/minion/equipment/location Id, plus one per damage
`BodyLocation` since damage cards have no individual name, plus a shared card back), built into
the app as WPF `Resource` items. Re-run it after adding or renaming a card:

```
pip install Pillow
python3 tools/generate_card_art.py
```

`CardFaceView` (`src/Gamma931.App/Controls`) is the reusable widget that renders a card's art plus
its name/flavor/effect text; `CardImageCatalog` + `CardToImagePathConverter` are what let any of
the six card model types bind straight to their art without each view re-deriving the path.

`RoundEngine` drives a `GameState` through RULES.md's round structure one explicit step at a
time (reveal location → draw equipment → draw boss → draw minions → combat cycle), so the WPF UI
presents each step interactively rather than auto-simulating a game. There is no crab action deck
— every crab in play (boss + minions) attacks each combat cycle, interleaved with each player's
equipment turn, until all crabs or all players are dead; see RULES.md's "Combat Turn Order". Most
character/boss abilities are still marked `TBD` in `ROSTER.md`; only the fully-specified ones
(Medic's passive/active) are wired into the engine, as a template for adding the rest once
they're designed.

## Building and running

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

**The WPF app (`Gamma931.App`) only builds on Windows** — WPF's build tooling
(`Microsoft.NET.Sdk.WindowsDesktop`) isn't available on Linux or macOS. On Windows:

```
dotnet build Gamma931.sln
dotnet run --project src/Gamma931.App
```

The Core library and its tests are plain .NET and build/run anywhere:

```
dotnet build src/Gamma931.Core/Gamma931.Core.csproj
dotnet test tests/Gamma931.Core.Tests/Gamma931.Core.Tests.csproj
```

## Editing card data

Each CSV in `/Data` maps 1:1 to a card type — see `src/Gamma931.Core/Data/CsvRows.cs` for the
exact column names expected. A few things to know:
- `locations.csv` has separate equipment-draw / minion-count columns per player count
  (2p/3p/4p/5p), since one location card serves every player count.
- `crab_bosses.csv`'s `Category` column must be `Universal` or `BiomeSpecific` (not a biome name)
  — the biome itself goes in the `Biome` column.
- Blank cells in `characters.csv`'s `ActiveUses*` columns mean that character's active-ability
  use count isn't balanced yet.

## Physical card assignments

For the physical (print-and-play) version of the game, every card artifact — characters,
crab bosses, crab minions, locations, equipment, and damage cards — maps to a
specific card across two standard 52-card decks (104 cards total, no jokers). That mapping is
recorded in [`Data/physical_card_assignments.csv`](Data/physical_card_assignments.csv):
`PhysicalCard` is `<Rank><Suit>-<Deck>` (e.g. `AS-A` = Ace of Spades, Deck A), and each row
also names which artifact (by category, ID, and name) that physical card represents. 76 of
the 104 current artifacts are assigned; the remaining 28 physical cards are marked
`Unused (Reserved)` for future content (including the 6 slots freed by dropping the crab
action deck).

## Balance simulation

`Data/locations.csv`'s `MinionCountXp` columns are tuned by a Monte Carlo combat simulator
in [`tools/`](tools) rather than picked by hand, targeting a ~70% full-campaign win rate per
player count. See [`BALANCE_NOTES.md`](BALANCE_NOTES.md) for the methodology, the explicit
assumptions used to fill in RULES.md's still-`TBD` mechanics (equipment targeting, etc.), and
an open finding that 2-player games can't reach 70% via minion count alone — re-run
`tools/balance_simulation.py` once those TBDs get real rules text. Note: the simulator predates
the no-crab-action-deck rules change (RULES.md's Combat Turn Order) and models the old
draw-a-crab-action cadence, so its win-rate numbers should be treated as stale until it's
updated to simulate "boss attacks, then players act, then every minion attacks" each cycle.
