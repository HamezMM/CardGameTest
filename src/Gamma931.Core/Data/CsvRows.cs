namespace Gamma931.Core.Data;

// Flat, mutable row shapes that mirror the CSV column headers exactly. CsvHelper binds to these
// by property name; CsvCardLoader then converts each row into the immutable domain model in
// Gamma931.Core.Models. Keeping the two separate means a column rename only touches this file.

public sealed class CharacterCsvRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Passive { get; set; } = string.Empty;
    public string Active { get; set; } = string.Empty;
    public int? ActiveUsesEasy { get; set; }
    public int? ActiveUsesNormal { get; set; }
    public int? ActiveUsesHard { get; set; }
}

public sealed class CrabBossCsvRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Biome { get; set; } = string.Empty;
    public string Concept { get; set; } = string.Empty;
    public string AbilityText { get; set; } = string.Empty;
    public string FlavorText { get; set; } = string.Empty;
    public int Hp2p { get; set; }
    public int Hp3p { get; set; }
    public int Hp4p { get; set; }
    public int Hp5p { get; set; }
    public int Minions2p { get; set; }
    public int Minions3p { get; set; }
    public int Minions4p { get; set; }
    public int Minions5p { get; set; }
}

public sealed class CrabMinionCsvRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FlavorText { get; set; } = string.Empty;
}

public sealed class DamageCsvRow
{
    public string Id { get; set; } = string.Empty;
    public string BodyLocation { get; set; } = string.Empty;
}

public sealed class EquipmentCsvRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EquipmentType { get; set; } = string.Empty;
    public string EffectText { get; set; } = string.Empty;
    public int DamageBonus { get; set; }
}

public sealed class LocationCsvRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Biome { get; set; } = string.Empty;
    public bool IsShuttle { get; set; }
    public string FlavorText { get; set; } = string.Empty;
    public int EquipmentDraw2p { get; set; }
    public int EquipmentDraw3p { get; set; }
    public int EquipmentDraw4p { get; set; }
    public int EquipmentDraw5p { get; set; }
}
