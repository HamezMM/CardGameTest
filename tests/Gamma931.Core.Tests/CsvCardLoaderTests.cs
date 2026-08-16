using Gamma931.Core.Data;
using Gamma931.Core.Models;
using Xunit;

namespace Gamma931.Core.Tests;

public class CsvCardLoaderTests
{
    private static string TestDataDirectory =>
        Path.Combine(AppContext.BaseDirectory, "TestData");

    [Fact]
    public void LoadFromDirectory_ReadsAllSixCardTypes()
    {
        var db = new CsvCardLoader().LoadFromDirectory(TestDataDirectory);

        Assert.NotEmpty(db.Characters);
        Assert.NotEmpty(db.Bosses);
        Assert.NotEmpty(db.Minions);
        Assert.NotEmpty(db.DamageCards);
        Assert.NotEmpty(db.Equipment);
        Assert.NotEmpty(db.Locations);
    }

    [Fact]
    public void LoadFromDirectory_ParsesMedicWithFullySpecifiedAbilities()
    {
        var db = new CsvCardLoader().LoadFromDirectory(TestDataDirectory);

        var medic = Assert.Single(db.Characters, c => c.Name == "Medic");
        Assert.Contains("+1 HP", medic.Passive);
        Assert.Equal(3, medic.ActiveUsesFor(DifficultyLevel.Easy));
        Assert.Equal(2, medic.ActiveUsesFor(DifficultyLevel.Normal));
        Assert.Equal(1, medic.ActiveUsesFor(DifficultyLevel.Hard));
    }

    [Fact]
    public void LoadFromDirectory_ParsesBossHpAndMinionCountByPlayerCount()
    {
        var db = new CsvCardLoader().LoadFromDirectory(TestDataDirectory);

        var ironshell = Assert.Single(db.Bosses, b => b.Name == "Ironshell");
        Assert.Equal(8, ironshell.HpFor(2));
        Assert.True(ironshell.HpFor(5) > ironshell.HpFor(2));
        Assert.True(ironshell.MinionCountFor(5) >= ironshell.MinionCountFor(2));

        Assert.All(db.Bosses, b => Assert.False(string.IsNullOrWhiteSpace(b.FlavorText)));
    }

    [Fact]
    public void LoadFromDirectory_ParsesFlavorTextForLocationsAndMinions()
    {
        var db = new CsvCardLoader().LoadFromDirectory(TestDataDirectory);

        Assert.All(db.Locations.Where(l => !l.IsShuttle), l => Assert.False(string.IsNullOrWhiteSpace(l.FlavorText)));
        Assert.All(db.Minions, m => Assert.False(string.IsNullOrWhiteSpace(m.FlavorText)));
    }

    [Fact]
    public void LoadFromDirectory_ParsesDamageValuesAndArmsDebuffPerRules()
    {
        var db = new CsvCardLoader().LoadFromDirectory(TestDataDirectory);

        Assert.All(db.DamageCards, d => Assert.Contains(d.Value, new[] { -1, -2 }));
        Assert.All(db.DamageCards.Where(d => d.ArmsDebuff), d => Assert.Equal(-1, d.Value));
        Assert.Equal(3, db.DamageCards.Count(d => d.ArmsDebuff));
    }

    [Fact]
    public void LoadFromDirectory_HasExactlyOneShuttleLocation()
    {
        var db = new CsvCardLoader().LoadFromDirectory(TestDataDirectory);

        var shuttle = db.ShuttleLocation;
        Assert.True(shuttle.IsShuttle);
        Assert.DoesNotContain(db.NonShuttleLocations, l => l.IsShuttle);
    }

    [Fact]
    public void LoadFromDirectory_ThrowsCardDataException_WhenDirectoryMissing()
    {
        Assert.Throws<CardDataException>(() =>
            new CsvCardLoader().LoadFromDirectory(Path.Combine(TestDataDirectory, "does-not-exist")));
    }

    [Fact]
    public void LoadFromDirectory_ThrowsCardDataException_OnInvalidEnumValue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            foreach (var file in Directory.GetFiles(TestDataDirectory, "*.csv"))
            {
                File.Copy(file, Path.Combine(tempDir, Path.GetFileName(file)));
            }

            File.WriteAllText(Path.Combine(tempDir, "crab_bosses.csv"),
                "Id,Name,Category,Biome,Concept,AbilityText,FlavorText,Hp2p,Hp3p,Hp4p,Hp5p,Minions2p,Minions3p,Minions4p,Minions5p\n" +
                "BOSS-99,Test,NotARealCategory,,,,,6,6,6,6,1,1,1,1\n");

            var ex = Assert.Throws<CardDataException>(() => new CsvCardLoader().LoadFromDirectory(tempDir));
            Assert.Contains("BOSS-99", ex.Message);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
