namespace Gamma931.Core.Models;

/// <summary>
/// A playable character archetype. Passive is always on; Active is a limited-use ability whose
/// remaining charges scale with difficulty (see RULES.md "Character Design"). ActiveUses* are
/// nullable because most active abilities are still un-numbered design placeholders (ROSTER.md).
/// </summary>
public sealed class CharacterCard
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Role { get; init; }
    public required string Passive { get; init; }
    public required string Active { get; init; }
    public int? ActiveUsesEasy { get; init; }
    public int? ActiveUsesNormal { get; init; }
    public int? ActiveUsesHard { get; init; }

    public int? ActiveUsesFor(DifficultyLevel difficulty) => difficulty switch
    {
        DifficultyLevel.Easy => ActiveUsesEasy,
        DifficultyLevel.Normal => ActiveUsesNormal,
        DifficultyLevel.Hard => ActiveUsesHard,
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty)),
    };

    public override string ToString() => Name;
}
