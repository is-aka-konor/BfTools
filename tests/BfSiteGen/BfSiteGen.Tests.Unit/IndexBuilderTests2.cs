using System.Text.Json;
using BfCommon.Domain.Models;
using BfSiteGen.Core.IO;
using BfSiteGen.Core.Publishing;
using BfSiteGen.Core.Services;
using NSubstitute;
using Xunit;

namespace BfSiteGen.Tests.Unit;

public class IndexBuilderDeterminismAndSearchTests
{
    [Fact]
    public void Index_Hash_Reproducible_And_Searchable_Documents()
    {
        var load = new ContentLoadResult();
        load.Talents.Add(new TalentDto { Slug = "tal1", Name = "Mage Hand", Category = "Magical", Description = "A spectral hand.", Src = new SourceRef { Abbr = "BF", Name = "Black Flag" } });
        load.Spells.Add(new SpellDto { Slug = "spell1", Name = "Fireball", Circle = 3, School = "Evocation", Effect = new() { "A bright streak flashes." }, Src = new SourceRef { Abbr = "BF", Name = "Black Flag" } });

        var dist = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dist);
        try
        {
            var idx1 = new IndexBuilder(new MarkdownRenderer()).BuildIndexes(load, dist);
            var idx2 = new IndexBuilder(new MarkdownRenderer()).BuildIndexes(load, dist);
            Assert.Equal(idx1["talents"].hash, idx2["talents"].hash);
            Assert.Equal(idx1["spells"].hash, idx2["spells"].hash);

            // Load talents index JSON and simulate a search by scanning documents
            var path = Path.Combine(dist, "index", $"talents-{idx1["talents"].hash}.minisearch.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var docs = doc.RootElement.GetProperty("documents").EnumerateArray().ToList();
            var found = docs.Any(d => d.GetProperty("name").GetString()!.Contains("Mage", StringComparison.OrdinalIgnoreCase) || d.GetProperty("descriptionHtml").GetString()!.Contains("hand", StringComparison.OrdinalIgnoreCase));
            Assert.True(found);
        }
        finally
        {
            try { Directory.Delete(dist, true); } catch { }
        }
    }
}

