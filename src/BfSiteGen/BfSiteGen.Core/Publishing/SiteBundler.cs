using System.Text.Json;
using BfCommon.Domain.Models;
using BfSiteGen.Core.IO;
using BfSiteGen.Core.Services;

namespace BfSiteGen.Core.Publishing;

public sealed class SiteBundler
{
    private readonly IContentReader _reader;

    public SiteBundler(IContentReader reader)
    {
        _reader = reader;
    }

    public BuildResult Build(string outputRoot, string distRoot)
    {
        var load = _reader.LoadAll(outputRoot);

        Directory.CreateDirectory(distRoot);
        var dataDir = Path.Combine(distRoot, "data");
        Directory.CreateDirectory(dataDir);

        var categories = new Dictionary<string, (string hash, int count)>();

        // Talents
        var talentsSorted = load.Talents.OrderBy(t => t.Slug, StringComparer.Ordinal).ToList();
        var talentsBytes = CanonicalJson.SerializeCanonicalArray(talentsSorted, CanonicalJson.WriteCanonicalTalent);
        var talentsHash = CanonicalJson.Sha256Hex(talentsBytes);
        var talentsFile = Path.Combine(dataDir, $"talents-{talentsHash}.json");
        File.WriteAllBytes(talentsFile, talentsBytes);
        categories["talents"] = (talentsHash, talentsSorted.Count);

        // Spells
        var spellsSorted = load.Spells.OrderBy(s => s.Slug, StringComparer.Ordinal).ToList();
        var spellsBytes = CanonicalJson.SerializeCanonicalArray(spellsSorted, CanonicalJson.WriteCanonicalSpell);
        var spellsHash = CanonicalJson.Sha256Hex(spellsBytes);
        var spellsFile = Path.Combine(dataDir, $"spells-{spellsHash}.json");
        File.WriteAllBytes(spellsFile, spellsBytes);
        categories["spells"] = (spellsHash, spellsSorted.Count);

        // Backgrounds
        var backgroundsSorted = load.Backgrounds.OrderBy(b => b.Slug, StringComparer.Ordinal).ToList();
        var backgroundsBytes = CanonicalJson.SerializeCanonicalArray(backgroundsSorted, CanonicalJson.WriteCanonicalBackground);
        var backgroundsHash = CanonicalJson.Sha256Hex(backgroundsBytes);
        var backgroundsFile = Path.Combine(dataDir, $"backgrounds-{backgroundsHash}.json");
        File.WriteAllBytes(backgroundsFile, backgroundsBytes);
        categories["backgrounds"] = (backgroundsHash, backgroundsSorted.Count);

        // Classes
        var classesSorted = load.Classes.OrderBy(c => c.Slug, StringComparer.Ordinal).ToList();
        var classesBytes = CanonicalJson.SerializeCanonicalArray(classesSorted, CanonicalJson.WriteCanonicalClass);
        var classesHash = CanonicalJson.Sha256Hex(classesBytes);
        var classesFile = Path.Combine(dataDir, $"classes-{classesHash}.json");
        File.WriteAllBytes(classesFile, classesBytes);
        categories["classes"] = (classesHash, classesSorted.Count);

        // Lineages
        var lineagesSorted = load.Lineages.OrderBy(l => l.Slug, StringComparer.Ordinal).ToList();
        var lineagesBytes = CanonicalJson.SerializeCanonicalArray(lineagesSorted, CanonicalJson.WriteCanonicalLineage);
        var lineagesHash = CanonicalJson.Sha256Hex(lineagesBytes);
        var lineagesFile = Path.Combine(dataDir, $"lineages-{lineagesHash}.json");
        File.WriteAllBytes(lineagesFile, lineagesBytes);
        categories["lineages"] = (lineagesHash, lineagesSorted.Count);

        // Build search indexes
        var indexBuilder = new IndexBuilder(new MarkdownRenderer());
        var indexMap = indexBuilder.BuildIndexes(load, distRoot);

        // Route stubs for static hosts (deep links)
        RouteStubGenerator.Generate(load, distRoot);

        // Manifest
        var manifestPath = Path.Combine(distRoot, "site-manifest.json");
        using (var fs = File.Create(manifestPath))
        using (var w = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartObject();
            w.WriteString("build", DateTime.UtcNow.ToString("O"));
            w.WritePropertyName("categories");
            w.WriteStartObject();
            foreach (var kv in categories.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                w.WritePropertyName(kv.Key);
                w.WriteStartObject();
                w.WriteString("hash", kv.Value.hash);
                w.WriteNumber("count", kv.Value.count);
                if (indexMap.TryGetValue(kv.Key, out var idx))
                    w.WriteString("indexHash", idx.hash);
                w.WriteEndObject();
            }
            w.WriteEndObject();

            // Aggregate unique sources across categories
            var allSources = new List<SourceRef>();
            allSources.AddRange(talentsSorted.Select(t => t.Src));
            allSources.AddRange(spellsSorted.Select(s => s.Src));
            allSources.AddRange(backgroundsSorted.Select(b => b.Src));
            allSources.AddRange(classesSorted.Select(c => c.Src));
            allSources.AddRange(lineagesSorted.Select(l => l.Src));
            var distinct = allSources
                .GroupBy(s => (s.Abbr, s.Name))
                .Select(g => g.First())
                .OrderBy(s => s.Abbr, StringComparer.Ordinal)
                .ThenBy(s => s.Name, StringComparer.Ordinal)
                .ToList();

            w.WritePropertyName("sources");
            w.WriteStartArray();
            foreach (var s in distinct)
            {
                w.WriteStartObject();
                w.WriteString("abbr", s.Abbr);
                w.WriteString("name", s.Name);
                w.WriteEndObject();
            }
            w.WriteEndArray();

            w.WriteEndObject();
        }

        return new BuildResult(categories, indexMap);
    }
}

public sealed class BuildResult
{
    public BuildResult(Dictionary<string, (string hash, int count)> categories, Dictionary<string, (string hash, int count)> indexes)
    {
        Categories = categories;
        Indexes = indexes;
    }

    public Dictionary<string, (string hash, int count)> Categories { get; }
    public Dictionary<string, (string hash, int count)> Indexes { get; }
}
