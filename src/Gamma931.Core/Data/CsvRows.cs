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
    public int StartingHp { get; set; }
}

public sealed class CrabMinionCsvRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class CrabActionCsvRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EffectText { get; set; } = string.Empty;
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
    public int DamageTier { get; set; }
}

public sealed class LocationCsvRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Biome { get; set; } = string.Empty;
    public bool IsShuttle { get; set; }
    public int EquipmentDraw2p { get; set; }
    public int EquipmentDraw3p { get; set; }
    public int EquipmentDraw4p { get; set; }
    public int EquipmentDraw5p { get; set; }
    public int CrabActionCount2p { get; set; }
    public int CrabActionCount3p { get; set; }
    public int CrabActionCount4p { get; set; }
    public int CrabActionCount5p { get; set; }
    public int MinionCount2p { get; set; }
    public int MinionCount3p { get; set; }
    public int MinionCount4p { get; set; }
    public int MinionCount5p { get; set; }
}
