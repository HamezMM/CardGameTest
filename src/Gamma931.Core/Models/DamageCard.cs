namespace Gamma931.Core.Models;

/// <summary>
/// Drawn to resolve a crab attack. RULES.md ("Damage & Health"): every damage card is a flat
/// -1 or -2 HP hit against the shared 6 HP pool — there's no per-body-location cost anymore.
/// A small subset of the -1 cards are flavored as Arms hits: on top of the HP loss, they leave
/// the target's attacks 1 point less effective (see Player.ArmsDebuffStacks) until they're
/// healed. Deck composition (how many of each) is what damage.csv tuning controls.
/// </summary>
public sealed class DamageCard
{
    public required string Id { get; init; }

    /// <summary>Always -1 or -2; the HP the target loses when this card is drawn.</summary>
    public required int Value { get; init; }

    /// <summary>True for the 3 Arms cards (RULES.md): also leaves the target's attacks 1 point
    /// less effective until healed, on top of the HP loss.</summary>
    public bool ArmsDebuff { get; init; }

    public int HpCost => -Value;

    /// <summary>Effect text shown on the card face; empty for a plain -1/-2 hit.</summary>
    public string ArmsNote => ArmsDebuff ? "Arms hit: attacks -1 until healed" : string.Empty;

    /// <summary>Small badge shown on the card face; empty for a plain -1/-2 hit.</summary>
    public string Tag => ArmsDebuff ? "ARMS" : string.Empty;

    public override string ToString() => ArmsDebuff ? $"{Value} (Arms)" : $"{Value}";
}
