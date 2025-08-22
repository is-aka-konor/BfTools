using System.Text.Json;
using BfSiteGen.Core.IO;
using BfSiteGen.Core.Models;
using BfSiteGen.Core.Publishing;
using Xunit;

namespace BfSiteGen.Tests.Unit;

public class IndexBuilderTests
{
    [Fact]
    public void Builds_Index_With_Fuzzy_And_Documents()
    {
        var load = new ContentLoadResult();
        load.Talents.Add(new Talent { Slug = "tal1", Name = "Mage Hand", Type = "Magical", DescriptionMd = "desc", DescriptionHtml = "<p>desc</p>", Sources = [ new SourceRef { Abbr = "BF", Name = "Black Flag" } ] });
        load.Spells.Add(new Spell { Slug = "spell1", Name = "Fireball", Circle = 3, School = "Evocation", CircleType = "Arcane", IsRitual = false, DescriptionMd = "explosion", DescriptionHtml = "<p>explosion</p>", Sources = [ new SourceRef { Abbr = "BF", Name = "Black Flag" } ] });

        var dist = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dist);
        try
        {
            var map = new IndexBuilder().BuildIndexes(load, dist);
            var tal = map["talents"].hash;
            var path = Path.Combine(dist, "index", $"talents-{tal}.minisearch.json");
            Assert.True(File.Exists(path));
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("options", out var opts));
            Assert.True(opts.GetProperty("searchOptions").TryGetProperty("fuzzy", out var f));
            Assert.True(f.GetDouble() > 0);
            var docs = root.GetProperty("documents");
            Assert.True(docs.GetArrayLength() >= 1);
            var first = docs.EnumerateArray().First();
            Assert.Equal("talents", first.GetProperty("category").GetString());
            Assert.Equal("tal1", first.GetProperty("slug").GetString());
            Assert.Equal("Mage Hand", first.GetProperty("name").GetString());
        }
        finally
        {
            try { Directory.Delete(dist, true); } catch { }
        }
    }
}

