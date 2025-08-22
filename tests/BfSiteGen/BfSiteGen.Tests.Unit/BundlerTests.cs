using BfSiteGen.Core.IO;
using BfSiteGen.Core.Models;
using BfSiteGen.Core.Publishing;
using NSubstitute;
using System.Text.Json;
using Xunit;

namespace BfSiteGen.Tests.Unit;

public class BundlerTests
{
    [Fact]
    public void Bundler_Emits_Bundles_Manifest_And_Stubs()
    {
        var load = new ContentLoadResult();
        load.Talents.Add(new Talent { Slug = "tal1", Name = "Talent 1", Type = "Magical", DescriptionMd = "md", DescriptionHtml = "<p>md</p>", Sources = [ new SourceRef { Abbr = "BF", Name = "Black Flag" } ] });
        load.Talents.Add(new Talent { Slug = "tal2", Name = "Talent 2", Type = "Martial", DescriptionMd = "md", DescriptionHtml = "<p>md</p>", Sources = [ new SourceRef { Abbr = "BF", Name = "Black Flag" } ] });
        load.Spells.Add(new Spell { Slug = "spell1", Name = "Spell 1", Circle = 1, School = "Evocation", CircleType = "Arcane", IsRitual = false, DescriptionMd = "md", DescriptionHtml = "<p>md</p>", Sources = [ new SourceRef { Abbr = "BF", Name = "Black Flag" } ] });

        var reader = Substitute.For<IContentReader>();
        reader.LoadAll(Arg.Any<string>()).Returns(load);

        var bundler = new SiteBundler(reader);
        var dist = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var res = bundler.Build("ignored", dist);
            // Data files exist
            var talentsEntry = res.Categories["talents"]; Assert.True(File.Exists(Path.Combine(dist, "data", $"talents-{talentsEntry.hash}.json")));
            var spellsEntry = res.Categories["spells"]; Assert.True(File.Exists(Path.Combine(dist, "data", $"spells-{spellsEntry.hash}.json")));
            // Index files exist
            var talentsIdx = res.Indexes["talents"]; Assert.True(File.Exists(Path.Combine(dist, "index", $"talents-{talentsIdx.hash}.minisearch.json")));
            // Manifest has categories
            var manifestPath = Path.Combine(dist, "site-manifest.json");
            Assert.True(File.Exists(manifestPath));
            using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
            var root = doc.RootElement;
            Assert.True(root.GetProperty("categories").TryGetProperty("talents", out var tEl));
            Assert.Equal(2, tEl.GetProperty("count").GetInt32());
            // Route stubs
            Assert.True(File.Exists(Path.Combine(dist, "spells", "spell1", "index.html")));
            Assert.True(File.Exists(Path.Combine(dist, "talents", "tal1", "index.html")));
        }
        finally
        {
            try { Directory.Delete(dist, true); } catch { }
        }
    }
}

