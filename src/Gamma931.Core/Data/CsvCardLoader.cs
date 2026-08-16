using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Gamma931.Core.Models;

namespace Gamma931.Core.Data;

/// <summary>
/// Loads the full <see cref="CardDatabase"/> from a directory of CSV files at app startup.
/// This is the extension point designers use to tweak card data: edit a CSV, relaunch the app.
/// </summary>
public sealed class CsvCardLoader
{
    private static readonly CsvConfiguration CsvConfig = new(CultureInfo.InvariantCulture)
    {
        HeaderValidated = null,
        MissingFieldFound = null,
    };

    public CardDatabase LoadFromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new CardDataException($"Card data directory not found: {directoryPath}");
        }

        return new CardDatabase
        {
            Characters = LoadCharacters(Path.Combine(directoryPath, "characters.csv")),
            Bosses = LoadBosses(Path.Combine(directoryPath, "crab_bosses.csv")),
            Minions = LoadMinions(Path.Combine(directoryPath, "crab_minions.csv")),
            CrabActions = LoadCrabActions(Path.Combine(directoryPath, "crab_actions.csv")),
            DamageCards = LoadDamageCards(Path.Combine(directoryPath, "damage.csv")),
            Equipment = LoadEquipment(Path.Combine(directoryPath, "equipment.csv")),
            Locations = LoadLocations(Path.Combine(directoryPath, "locations.csv")),
        };
    }

    private List<CharacterCard> LoadCharacters(string path) =>
        ReadRows<CharacterCsvRow, CharacterCard>(path, row => new CharacterCard
        {
            Id = row.Id,
            Name = row.Name,
            Role = row.Role,
            Passive = row.Passive,
            Active = row.Active,
            ActiveUsesEasy = row.ActiveUsesEasy,
            ActiveUsesNormal = row.ActiveUsesNormal,
            ActiveUsesHard = row.ActiveUsesHard,
        });

    private List<CrabBossCard> LoadBosses(string path) =>
        ReadRows<CrabBossCsvRow, CrabBossCard>(path, row => new CrabBossCard
        {
            Id = row.Id,
            Name = row.Name,
            Category = ParseEnum<BossCategory>(row.Category, path, row.Id),
            Biome = row.Biome,
            Concept = row.Concept,
            AbilityText = row.AbilityText,
            FlavorText = row.FlavorText,
            HpByPlayerCount = new Dictionary<int, int>
            {
                [2] = row.Hp2p,
                [3] = row.Hp3p,
                [4] = row.Hp4p,
                [5] = row.Hp5p,
            },
            MinionCountByPlayerCount = new Dictionary<int, int>
            {
                [2] = row.Minions2p,
                [3] = row.Minions3p,
                [4] = row.Minions4p,
                [5] = row.Minions5p,
            },
        });

    private List<CrabMinionCard> LoadMinions(string path) =>
        ReadRows<CrabMinionCsvRow, CrabMinionCard>(path, row => new CrabMinionCard
        {
            Id = row.Id,
            Name = row.Name,
            FlavorText = row.FlavorText,
        });

    private List<CrabActionCard> LoadCrabActions(string path) =>
        ReadRows<CrabActionCsvRow, CrabActionCard>(path, row => new CrabActionCard
        {
            Id = row.Id,
            Name = row.Name,
            EffectText = row.EffectText,
        });

    private List<DamageCard> LoadDamageCards(string path) =>
        ReadRows<DamageCsvRow, DamageCard>(path, row => new DamageCard
        {
            Id = row.Id,
            BodyLocation = ParseEnum<BodyLocation>(row.BodyLocation, path, row.Id),
        });

    private List<EquipmentCard> LoadEquipment(string path) =>
        ReadRows<EquipmentCsvRow, EquipmentCard>(path, row => new EquipmentCard
        {
            Id = row.Id,
            Name = row.Name,
            EquipmentType = ParseEnum<EquipmentType>(row.EquipmentType, path, row.Id),
            EffectText = row.EffectText,
            DamageBonus = row.DamageBonus,
        });

    private List<LocationCard> LoadLocations(string path) =>
        ReadRows<LocationCsvRow, LocationCard>(path, row => new LocationCard
        {
            Id = row.Id,
            Name = row.Name,
            Biome = row.Biome,
            IsShuttle = row.IsShuttle,
            FlavorText = row.FlavorText,
            EquipmentDrawByPlayerCount = new Dictionary<int, int>
            {
                [2] = row.EquipmentDraw2p,
                [3] = row.EquipmentDraw3p,
                [4] = row.EquipmentDraw4p,
                [5] = row.EquipmentDraw5p,
            },
            CrabActionCountByPlayerCount = new Dictionary<int, int>
            {
                [2] = row.CrabActionCount2p,
                [3] = row.CrabActionCount3p,
                [4] = row.CrabActionCount4p,
                [5] = row.CrabActionCount5p,
            },
        });

    private static List<TModel> ReadRows<TRow, TModel>(string path, Func<TRow, TModel> toModel)
    {
        if (!File.Exists(path))
        {
            throw new CardDataException($"Card data file not found: {path}");
        }

        try
        {
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, CsvConfig);
            var rows = csv.GetRecords<TRow>().ToList();
            return rows.Select(toModel).ToList();
        }
        catch (CardDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CardDataException($"Failed to read {Path.GetFileName(path)}: {ex.Message}", ex);
        }
    }

    private static TEnum ParseEnum<TEnum>(string value, string path, string cardId) where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var result))
        {
            return result;
        }

        throw new CardDataException(
            $"{Path.GetFileName(path)}: card '{cardId}' has invalid {typeof(TEnum).Name} value '{value}'. " +
            $"Expected one of: {string.Join(", ", Enum.GetNames<TEnum>())}.");
    }
}
