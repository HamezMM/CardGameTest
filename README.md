# Gamma-931 — Digital Playtester

A C# WPF desktop app for playtesting *Gamma-931*, a co-op roguelike card game for 2-5 players.
See [`RULES.md`](RULES.md) and [`ROSTER.md`](ROSTER.md) for the game design this app implements.

## Why CSV

Every card (characters, bosses, minions, crab actions, damage, equipment, locations) is loaded
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
tests/
  Gamma931.Core.Tests/        xUnit tests for CSV loading and the round engine
```

`RoundEngine` drives a `GameState` through RULES.md's round structure one explicit step at a
time (reveal location → draw equipment → draw boss → set aside crab actions → draw minions →
combat cycle → End Phase Combat), so the WPF UI presents each step interactively rather than
auto-simulating a game. Most character/boss abilities are still marked `TBD` in `ROSTER.md`; only
the fully-specified ones (Medic's passive/active) are wired into the engine, as a template for
adding the rest once they're designed.

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
- `locations.csv` has separate equipment-draw / crab-action / minion-count columns per player
  count (2p/3p/4p/5p), since one location card serves every player count.
- `crab_bosses.csv`'s `Category` column must be `Universal` or `BiomeSpecific` (not a biome name)
  — the biome itself goes in the `Biome` column.
- Blank cells in `characters.csv`'s `ActiveUses*` columns mean that character's active-ability
  use count isn't balanced yet.

## Physical card assignments

For the physical (print-and-play) version of the game, every card artifact — characters,
crab bosses, crab minions, crab actions, locations, equipment, and damage cards — maps to a
specific card across two standard 52-card decks (104 cards total, no jokers). That mapping is
recorded in [`Data/physical_card_assignments.csv`](Data/physical_card_assignments.csv):
`PhysicalCard` is `<Rank><Suit>-<Deck>` (e.g. `AS-A` = Ace of Spades, Deck A), and each row
also names which artifact (by category, ID, and name) that physical card represents. 83 of
the 104 current artifacts are assigned; the remaining 21 physical cards are marked
`Unused (Reserved)` for future content.

## Balance simulation

`Data/locations.csv`'s `MinionCountXp` columns are tuned by a Monte Carlo combat simulator
in [`tools/`](tools) rather than picked by hand, targeting a ~70% full-campaign win rate per
player count. See [`BALANCE_NOTES.md`](BALANCE_NOTES.md) for the methodology, the explicit
assumptions used to fill in RULES.md's still-`TBD` mechanics (crab action effects, equipment
targeting, etc.), and an open finding that 2-player games can't reach 70% via minion count
alone — re-run `tools/balance_simulation.py` once those TBDs get real rules text.
