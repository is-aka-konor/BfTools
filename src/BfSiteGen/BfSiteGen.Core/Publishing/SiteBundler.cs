using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BfCommon.Domain.Models;
using BfSiteGen.Core.IO;
using BfSiteGen.Core.Rendering;
using BfSiteGen.Core.Services;

namespace BfSiteGen.Core.Publishing;

public sealed partial class SiteBundler
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
        var (talentsHash, talentsCount) = WriteCategory(dataDir, "talents", talentsSorted, WriteTalent);
        categories["talents"] = (talentsHash, talentsCount);

        // Spells
        var spellsSorted = load.Spells.OrderBy(s => s.Slug, StringComparer.Ordinal).ToList();
        var (spellsHash, spellsCount) = WriteCategory(dataDir, "spells", spellsSorted, WriteSpell);
        categories["spells"] = (spellsHash, spellsCount);

        // Backgrounds
        var backgroundsSorted = load.Backgrounds.OrderBy(b => b.Slug, StringComparer.Ordinal).ToList();
        var (backgroundsHash, backgroundsCount) = WriteCategory(dataDir, "backgrounds", backgroundsSorted, WriteBackground);
        categories["backgrounds"] = (backgroundsHash, backgroundsCount);

        // Classes
        var classesSorted = load.Classes.OrderBy(c => c.Slug, StringComparer.Ordinal).ToList();
        var (classesHash, classesCount) = WriteCategory(dataDir, "classes", classesSorted, WriteClass);
        categories["classes"] = (classesHash, classesCount);

        // Lineages
        var lineagesSorted = load.Lineages.OrderBy(l => l.Slug, StringComparer.Ordinal).ToList();
        var (lineagesHash, lineagesCount) = WriteCategory(dataDir, "lineages", lineagesSorted, WriteLineage);
        categories["lineages"] = (lineagesHash, lineagesCount);

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

// Helpers for streaming, on-the-fly HTML generation
partial class SiteBundler
{
    private static void WriteSrc(Utf8JsonWriter w, SourceRef src)
    {
        w.WritePropertyName("src");
        w.WriteStartObject();
        w.WriteString("abbr", src.Abbr);
        w.WriteString("name", src.Name);
        if (!string.IsNullOrEmpty(src.Version)) w.WriteString("version", src.Version);
        if (!string.IsNullOrEmpty(src.Url)) w.WriteString("url", src.Url);
        if (!string.IsNullOrEmpty(src.License)) w.WriteString("license", src.License);
        if (!string.IsNullOrEmpty(src.Hash)) w.WriteString("hash", src.Hash);
        w.WriteEndObject();
    }

    private (string hash, int count) WriteCategory<T>(string dataDir, string category, IReadOnlyList<T> items, Action<Utf8JsonWriter, T> writeItem)
        where T : BaseEntity
    {
        Directory.CreateDirectory(dataDir);
        var tmp = Path.Combine(dataDir, $"{category}-tmp-{Guid.NewGuid():N}.json");

        using (var fs = File.Create(tmp))
        using (var w = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = false, SkipValidation = false }))
        {
            w.WriteStartArray();
            foreach (var it in items)
            {
                writeItem(w, it);
            }
            w.WriteEndArray();
            w.Flush();
        }

        // Compute hash streaming from file to avoid full in-memory bytes
        using var sha = SHA256.Create();
        using (var fs = File.OpenRead(tmp))
        {
            var hash = sha.ComputeHash(fs);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            var hex = sb.ToString();
            var finalPath = Path.Combine(dataDir, $"{category}-{hex}.json");
            if (File.Exists(finalPath)) File.Delete(finalPath);
            File.Move(tmp, finalPath);
            return (hex, items.Count);
        }
    }

    private void WriteSpell(Utf8JsonWriter w, SpellDto s)
    {
        var r = new MarkdownRenderer();
        w.WriteStartObject();
        w.WriteNumber("circle", s.Circle);
        w.WriteString("school", s.School);
        w.WriteString("castingTime", s.CastingTime);
        w.WriteString("range", s.Range);
        w.WriteString("components", s.Components);
        w.WriteString("duration", s.Duration);
        if (s.Circles is { Count: > 0 }) { w.WritePropertyName("circles"); w.WriteStartArray(); foreach (var c in s.Circles) w.WriteStringValue(c); w.WriteEndArray(); }
        if (s.Effect is { Count: > 0 }) { w.WritePropertyName("effect"); w.WriteStartArray(); foreach (var e in s.Effect) w.WriteStringValue(e); w.WriteEndArray(); }

        // Rendered HTML from structured Effect or fallback Description
        w.WriteString("descriptionHtml", s.ToHtml(r));

        // Common
        w.WriteString("type", s.Type);
        w.WriteString("id", s.Id);
        w.WriteString("slug", s.Slug);
        w.WriteString("name", s.Name);
        w.WriteString("schemaVersion", s.SchemaVersion);
        WriteSrc(w, s.Src);
        if (!string.IsNullOrWhiteSpace(s.Summary)) w.WriteString("summary", s.Summary);
        if (!string.IsNullOrWhiteSpace(s.SourceFile)) w.WriteString("sourceFile", s.SourceFile);
        w.WriteEndObject();
    }

    private void WriteTalent(Utf8JsonWriter w, TalentDto t)
    {
        var r = new MarkdownRenderer();
        w.WriteStartObject();
        // Rendered HTML from description + benefits
        w.WriteString("descriptionHtml", t.ToHtml(r));
        // Talent-specific fields
        if (!string.IsNullOrWhiteSpace(t.Category)) w.WriteString("category", t.Category);
        if (!string.IsNullOrWhiteSpace(t.Requirement)) w.WriteString("requirement", t.Requirement);
        if (t.Benefits is { Count: > 0 }) { w.WritePropertyName("benefits"); w.WriteStartArray(); foreach (var b in t.Benefits) w.WriteStringValue(b); w.WriteEndArray(); }
        // Common
        w.WriteString("type", t.Type);
        w.WriteString("id", t.Id);
        w.WriteString("slug", t.Slug);
        w.WriteString("name", t.Name);
        w.WriteString("schemaVersion", t.SchemaVersion);
        WriteSrc(w, t.Src);
        if (!string.IsNullOrWhiteSpace(t.Summary)) w.WriteString("summary", t.Summary);
        if (!string.IsNullOrWhiteSpace(t.SourceFile)) w.WriteString("sourceFile", t.SourceFile);
        w.WriteEndObject();
    }

    private void WriteBackground(Utf8JsonWriter w, BackgroundDto b)
    {
        var r = new MarkdownRenderer();
        w.WriteStartObject();
        w.WriteString("descriptionHtml", b.ToHtml(r));
        // Keep DTO fields
        if (!string.IsNullOrWhiteSpace(b.Description)) w.WriteString("description", b.Description);
        w.WriteString("type", b.Type);
        w.WriteString("id", b.Id);
        w.WriteString("slug", b.Slug);
        w.WriteString("name", b.Name);
        w.WriteString("schemaVersion", b.SchemaVersion);
        WriteSrc(w, b.Src);
        if (!string.IsNullOrWhiteSpace(b.Summary)) w.WriteString("summary", b.Summary);
        if (!string.IsNullOrWhiteSpace(b.SourceFile)) w.WriteString("sourceFile", b.SourceFile);
        w.WriteEndObject();
    }

    private void WriteClass(Utf8JsonWriter w, ClassDto c)
    {
        var r = new MarkdownRenderer();
        w.WriteStartObject();
        w.WriteString("descriptionHtml", c.ToHtml(r));
        if (!string.IsNullOrWhiteSpace(c.Description)) w.WriteString("description", c.Description);
        w.WriteString("type", c.Type);
        w.WriteString("id", c.Id);
        w.WriteString("slug", c.Slug);
        w.WriteString("name", c.Name);
        w.WriteString("schemaVersion", c.SchemaVersion);
        WriteSrc(w, c.Src);
        if (!string.IsNullOrWhiteSpace(c.Summary)) w.WriteString("summary", c.Summary);
        if (!string.IsNullOrWhiteSpace(c.SourceFile)) w.WriteString("sourceFile", c.SourceFile);
        w.WriteEndObject();
    }

    private void WriteLineage(Utf8JsonWriter w, LineageDto l)
    {
        var r = new MarkdownRenderer();
        w.WriteStartObject();
        w.WriteString("descriptionHtml", l.ToHtml(r));
        w.WriteString("size", l.Size);
        w.WriteNumber("speed", l.Speed);
        if (l.Traits is { Count: > 0 })
        {
            w.WritePropertyName("traits");
            w.WriteStartArray();
            foreach (var t in l.Traits)
            {
                w.WriteStartObject();
                w.WriteString("name", t.Name);
                w.WriteString("description", t.Description);
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        w.WriteString("type", l.Type);
        w.WriteString("id", l.Id);
        w.WriteString("slug", l.Slug);
        w.WriteString("name", l.Name);
        w.WriteString("schemaVersion", l.SchemaVersion);
        WriteSrc(w, l.Src);
        if (!string.IsNullOrWhiteSpace(l.Summary)) w.WriteString("summary", l.Summary);
        if (!string.IsNullOrWhiteSpace(l.SourceFile)) w.WriteString("sourceFile", l.SourceFile);
        w.WriteEndObject();
    }
}
