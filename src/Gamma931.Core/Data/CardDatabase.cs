using Gamma931.Core.Models;

namespace Gamma931.Core.Data;

/// <summary>
/// The full set of card data loaded from CSV at startup. Immutable for the lifetime of the app;
/// to playtest a data tweak, edit the CSV and relaunch.
/// </summary>
public sealed class CardDatabase
{
    public required IReadOnlyList<CharacterCard> Characters { get; init; }
    public required IReadOnlyList<CrabBossCard> Bosses { get; init; }
    public required IReadOnlyList<CrabMinionCard> Minions { get; init; }
    public required IReadOnlyList<DamageCard> DamageCards { get; init; }
    public required IReadOnlyList<EquipmentCard> Equipment { get; init; }
    public required IReadOnlyList<LocationCard> Locations { get; init; }

    public IReadOnlyList<LocationCard> NonShuttleLocations =>
        Locations.Where(l => !l.IsShuttle).ToList();

    public LocationCard ShuttleLocation =>
        Locations.SingleOrDefault(l => l.IsShuttle)
        ?? throw new InvalidOperationException("locations.csv must contain exactly one row with IsShuttle=true.");
}
