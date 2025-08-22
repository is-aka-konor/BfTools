using System.Text.Json;
using BfSiteGen.Core.IO;
using BfSiteGen.Core.Services;
using Xunit;

namespace BfSiteGen.Tests.Unit;

public class ContentReaderTests
{
    [Fact]
    public void LoadAll_Parses_Items_And_Validates_Missing_Fields()
    {
        // Arrange: create a temp data layout under current dir to avoid OS temp.
        var root = Path.Combine(Directory.GetCurrentDirectory(), "testdata", Guid.NewGuid().ToString("N"));
        var talentsDir = Path.Combine(root, "data", "talents");
        var spellsDir = Path.Combine(root, "data", "spells");
        Directory.CreateDirectory(talentsDir);
        Directory.CreateDirectory(spellsDir);

        // Talent has description (as 'description'), but is missing required 'type'.
        var talent = new
        {
            slug = "sample-talent",
            name = "Sample Talent",
            description = "#### H\n\n* a\n* b",
            sources = new[] { new { abbr = "BFRD", name = "Black Flag Reference Document" } }
        };
        File.WriteAllText(Path.Combine(talentsDir, "talent.json"), JsonSerializer.Serialize(talent));

        // Spell provides circle and school but misses circleType and descriptionMd
        var spell = new
        {
            slug = "sample-spell",
            name = "Sample Spell",
            circle = 1,
            school = "Evocation",
            src = new { abbr = "BFRD", name = "Black Flag Reference Document" }
        };
        File.WriteAllText(Path.Combine(spellsDir, "spell.json"), JsonSerializer.Serialize(spell));

        var reader = new ContentReader(new MarkdownRenderer());

        // Act
        var result = reader.LoadAll(root);

        // Assert
        Assert.Single(result.Talents);
        Assert.Single(result.Spells);
        Assert.NotEmpty(result.Errors);

        // Talent rendered HTML, contains a list
        var t = result.Talents[0];
        Assert.Contains("<ul>", t.DescriptionHtml);

        // Expect validation errors for missing fields
        Assert.Contains(result.Errors, e => e.Field == "type" && e.Category == "talents");
        Assert.Contains(result.Errors, e => e.Field == "descriptionMd" && e.Category == "spells");
        Assert.Contains(result.Errors, e => e.Field == "circleType" && e.Category == "spells");
    }
}

