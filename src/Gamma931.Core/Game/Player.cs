using Gamma931.Core.Models;

namespace Gamma931.Core.Game;

/// <summary>
/// A player's live state during a game. HP is a single shared pool of <see cref="MaxHp"/>
/// (RULES.md "Damage & Health") — not tracked per limb.
/// </summary>
public sealed class Player
{
    public const int MaxHp = 6;

    public required string Name { get; init; }
    public required CharacterCard Character { get; init; }
    public int CurrentHp { get; private set; } = MaxHp;

    /// <summary>Players default to Range position at the start of each round (RULES.md).</summary>
    public Position Position { get; set; } = Position.Range;

    public List<EquipmentCard> Hand { get; } = new();

    public int? ActiveUsesRemaining { get; set; }

    public bool IsAlive => CurrentHp > 0;

    public void TakeDamage(int amount)
    {
        CurrentHp = Math.Max(0, CurrentHp - amount);
    }

    public void Heal(int amount)
    {
        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
    }

    public void ResetForNewRound()
    {
        Position = Position.Range;
    }
}
