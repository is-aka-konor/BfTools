using System.Text.Json;
using BfSiteGen.Core.IO;
using BfSiteGen.Core.Publishing;
using BfSiteGen.Core.Services;
using Xunit;

namespace BfSiteGen.Tests.Integration;

public class GenerationIntegrationTests
{
    private static string FindRepoRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        for (var i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(dir, "output"))) return dir;
            dir = Path.GetDirectoryName(dir)!;
        }
        throw new DirectoryNotFoundException("Could not find repo root with 'output' folder.");
    }

    private static string PrepareMiniDataset()
    {
        var repo = FindRepoRoot();
        var srcRoot = Path.Combine(repo, "output", "data");
        var mini = Path.Combine(Path.GetTempPath(), "bfmini-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(mini, "data", "spells"));
        Directory.CreateDirectory(Path.Combine(mini, "data", "talents"));

        // Copy one spell and one talent from real dataset
        var spell = Directory.EnumerateFiles(Path.Combine(srcRoot, "spells"), "*.json").First();
        var talent = Directory.EnumerateFiles(Path.Combine(srcRoot, "talents"), "*.json").First();
        File.Copy(spell, Path.Combine(mini, "data", "spells", Path.GetFileName(spell)));
        File.Copy(talent, Path.Combine(mini, "data", "talents", Path.GetFileName(talent)));
        return mini;
    }

    [Fact]
    public void E2E_Builds_And_Detects_Category_Scoped_Change()
    {
        var mini = PrepareMiniDataset();
        var dist1 = Path.Combine(Path.GetTempPath(), "bfdist1-" + Guid.NewGuid().ToString("N"));
        var dist2 = Path.Combine(Path.GetTempPath(), "bfdist2-" + Guid.NewGuid().ToString("N"));
        try
        {
            var bundler = new SiteBundler(new ContentReader());
            var res1 = bundler.Build(mini, dist1);

            // Assert manifest and bundle files exist
            var manifest1 = JsonDocument.Parse(File.ReadAllText(Path.Combine(dist1, "site-manifest.json"))).RootElement;
            var cats1 = manifest1.GetProperty("categories");
            Assert.True(cats1.TryGetProperty("spells", out var spells1));
            Assert.True(cats1.TryGetProperty("talents", out var talents1));
            var spellsHash1 = spells1.GetProperty("hash").GetString()!;
            var talentsHash1 = talents1.GetProperty("hash").GetString()!;
            Assert.True(File.Exists(Path.Combine(dist1, "data", $"spells-{spellsHash1}.json")));
            Assert.True(File.Exists(Path.Combine(dist1, "index", $"spells-{spells1.GetProperty("indexHash").GetString()}.minisearch.json")));
            Assert.True(File.Exists(Path.Combine(dist1, "data", $"talents-{talentsHash1}.json")));

            // Mutate the spell file to trigger only spells changes
            var spellPath = Directory.EnumerateFiles(Path.Combine(mini, "data", "spells"), "*.json").First();
            // Mutate content semantically so canonical hash changes: tweak school
            var txt = File.ReadAllText(spellPath);
            var node = System.Text.Json.Nodes.JsonNode.Parse(txt)!.AsObject();
            var school = node["school"]?.GetValue<string>() ?? string.Empty;
            node["school"] = school + " ";
            File.WriteAllText(spellPath, node.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));

            var res2 = bundler.Build(mini, dist2);
            var manifest2 = JsonDocument.Parse(File.ReadAllText(Path.Combine(dist2, "site-manifest.json"))).RootElement;
            var cats2 = manifest2.GetProperty("categories");
            var spells2 = cats2.GetProperty("spells");
            var talents2 = cats2.GetProperty("talents");
            var spellsHash2 = spells2.GetProperty("hash").GetString()!;
            var talentsHash2 = talents2.GetProperty("hash").GetString()!;

            // Only spells hashes change
            Assert.NotEqual(spellsHash1, spellsHash2);
            Assert.Equal(talentsHash1, talentsHash2);

            // New spells files exist in dist2
            Assert.True(File.Exists(Path.Combine(dist2, "data", $"spells-{spellsHash2}.json")));
            Assert.True(File.Exists(Path.Combine(dist2, "index", $"spells-{spells2.GetProperty("indexHash").GetString()}.minisearch.json")));
        }
        finally
        {
            try { Directory.Delete(mini, true); } catch { }
            try { Directory.Delete(dist1, true); } catch { }
            try { Directory.Delete(dist2, true); } catch { }
        }
    }
}
