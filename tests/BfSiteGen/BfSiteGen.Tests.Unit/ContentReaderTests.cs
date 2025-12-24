using System.Text.Json;
using BfSiteGen.Core.IO;
using BfSiteGen.Core.Services;
using Xunit;

namespace BfSiteGen.Tests.Unit;

public class ContentReaderTests
{
    [Fact]
    public void LoadAll_Parses_Items_And_Validates_Basics()
    {
        // Arrange: create a temp data layout under current dir to avoid OS temp.
        var root = Path.Combine(Directory.GetCurrentDirectory(), "testdata", Guid.NewGuid().ToString("N"));
        var talentsDir = Path.Combine(root, "data", "talents");
        var spellsDir = Path.Combine(root, "data", "spells");
        Directory.CreateDirectory(talentsDir);
        Directory.CreateDirectory(spellsDir);

        // Talent matches shared DTO shape (single src)
        var talent = new
        {
            slug = "sample-talent",
            name = "Sample Talent",
            description = "#### H\n\n* a\n* b",
            category = "Magical",
            src = new { abbr = "BFRD", name = "Black Flag Reference Document" }
        };
        File.WriteAllText(Path.Combine(talentsDir, "talent.json"), JsonSerializer.Serialize(talent));

        // Spell provides minimal required fields for shared DTO
        var spell = new
        {
            slug = "sample-spell",
            name = "Sample Spell",
            circle = 1,
            school = "Evocation",
            description = "Spell md",
            effect = new[] { "Boom" },
            src = new { abbr = "BFRD", name = "Black Flag Reference Document" }
        };
        File.WriteAllText(Path.Combine(spellsDir, "spell.json"), JsonSerializer.Serialize(spell));

        // Invalid spell missing circle
        var badSpell = new
        {
            slug = "bad-spell",
            name = "Bad Spell",
            circle = -1,
            school = "Evocation",
            description = "Bad md",
            effect = new[] { "Fizz" },
            src = new { abbr = "BFRD", name = "Black Flag Reference Document" }
        };
        File.WriteAllText(Path.Combine(spellsDir, "bad.json"), JsonSerializer.Serialize(badSpell));

        var reader = new ContentReader();

        // Act
        var result = reader.LoadAll(root);

        // Assert
        Assert.Single(result.Talents);
        Assert.Equal(2, result.Spells.Count);
        Assert.Contains(result.Errors, e => e.Category == "spells" && e.Field == "circle" && e.Slug == "bad-spell");
    }
}
