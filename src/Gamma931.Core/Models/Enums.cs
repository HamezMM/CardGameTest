namespace Gamma931.Core.Models;

/// <summary>Combat stance a player occupies. Set dynamically by the equipment they play.</summary>
public enum Position
{
    Range,
    Melee,
}

public enum EquipmentType
{
    Melee,
    Ranged,
    Protection,
    Healing,
}

public enum BossCategory
{
    Universal,
    BiomeSpecific,
}

/// <summary>Scales limited-use active-ability counts; harder games grant fewer uses.</summary>
public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard,
}
