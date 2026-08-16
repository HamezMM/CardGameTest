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
    public void LoadFromDirectory_ParsesBodyLocationHpCostsPerRules()
    {
        var db = new CsvCardLoader().LoadFromDirectory(TestDataDirectory);

        Assert.All(db.DamageCards.Where(d => d.BodyLocation == BodyLocation.Arms), d => Assert.Equal(1, d.HpCost));
        Assert.All(db.DamageCards.Where(d => d.BodyLocation == BodyLocation.Head), d => Assert.Equal(2, d.HpCost));
        Assert.All(db.DamageCards.Where(d => d.BodyLocation == BodyLocation.Legs), d => Assert.Equal(1, d.HpCost));
        Assert.All(db.DamageCards.Where(d => d.BodyLocation == BodyLocation.Torso), d => Assert.Equal(1, d.HpCost));
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

            File.WriteAllText(Path.Combine(tempDir, "damage.csv"), "Id,BodyLocation\nDMG-99,NotARealLocation\n");

            var ex = Assert.Throws<CardDataException>(() => new CsvCardLoader().LoadFromDirectory(tempDir));
            Assert.Contains("DMG-99", ex.Message);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
