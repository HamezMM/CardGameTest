namespace Gamma931.Core.Models;

/// <summary>Body location a damage card targets. HP cost is fixed by RULES.md.</summary>
public enum BodyLocation
{
    Arms,
    Head,
    Legs,
    Torso,
}

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
    Utility,
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
