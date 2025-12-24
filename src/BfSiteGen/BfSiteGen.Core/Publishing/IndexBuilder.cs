using System.Text.Json;
using BfCommon.Domain.Models;
using BfSiteGen.Core.IO;
using BfSiteGen.Core.Services;

namespace BfSiteGen.Core.Publishing;

public sealed class IndexBuilder
{
    public sealed class IndexConfig
    {
        public string[] Fields { get; init; } = new[] { "name", "description" };
        public Dictionary<string, double> Boost { get; init; } = new() { { "name", 3.0 }, { "description", 1.0 } };
        public double? Fuzzy { get; init; } = 0.2; // MiniSearch fuzzy ratio
        public string[] StoreFields { get; init; } = new[] { "slug", "category", "sources", "circle", "school", "isRitual", "type" };
    }

    private readonly IndexConfig _config;
    private readonly IMarkdownRenderer _markdown;

    public IndexBuilder(IMarkdownRenderer markdown, IndexConfig? config = null)
    {
        _markdown = markdown;
        _config = config ?? new IndexConfig();
    }

    public Dictionary<string, (string hash, int count)> BuildIndexes(ContentLoadResult load, string distRoot)
    {
        var indexDir = Path.Combine(distRoot, "index");
        Directory.CreateDirectory(indexDir);
        var map = new Dictionary<string, (string, int)>();

        map["spells"] = BuildForCategory(indexDir, "spells", load.Spells.OrderBy(s => s.Slug, StringComparer.Ordinal), WriteSpellDoc);
        map["talents"] = BuildForCategory(indexDir, "talents", load.Talents.OrderBy(t => t.Slug, StringComparer.Ordinal), WriteTalentDoc);
        map["backgrounds"] = BuildForCategory(indexDir, "backgrounds", load.Backgrounds.OrderBy(b => b.Slug, StringComparer.Ordinal), WriteBackgroundDoc);
        map["classes"] = BuildForCategory(indexDir, "classes", load.Classes.OrderBy(c => c.Slug, StringComparer.Ordinal), WriteClassDoc);
        map["lineages"] = BuildForCategory(indexDir, "lineages", load.Lineages.OrderBy(l => l.Slug, StringComparer.Ordinal), WriteLineageDoc);

        return map;
    }

    private (string hash, int count) BuildForCategory<T>(string indexDir, string category, IEnumerable<T> items, Action<Utf8JsonWriter, string, T> writeDoc)
    {
        using var ms = new MemoryStream();
        using var w = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        w.WriteStartObject();
        w.WritePropertyName("options");
        WriteOptions(w, _config);

        w.WritePropertyName("documents");
        w.WriteStartArray();
        int count = 0;
        foreach (var item in items)
        {
            writeDoc(w, category, item);
            count++;
        }
        w.WriteEndArray();
        w.WriteEndObject();
        w.Flush();

        var bytes = ms.ToArray();
        var hash = CanonicalJson.Sha256Hex(bytes);
        var path = Path.Combine(indexDir, $"{category}-{hash}.minisearch.json");
        File.WriteAllBytes(path, bytes);
        return (hash, count);
    }

    private static void WriteOptions(Utf8JsonWriter w, IndexConfig c)
    {
        w.WriteStartObject();
        w.WritePropertyName("fields");
        w.WriteStartArray();
        foreach (var f in c.Fields)
            w.WriteStringValue(f);
        w.WriteEndArray();

        w.WritePropertyName("storeFields");
        w.WriteStartArray();
        foreach (var f in c.StoreFields)
            w.WriteStringValue(f);
        w.WriteEndArray();

        w.WritePropertyName("searchOptions");
        w.WriteStartObject();
        w.WritePropertyName("boost");
        w.WriteStartObject();
        foreach (var kv in c.Boost.OrderBy(k => k.Key, StringComparer.Ordinal))
            w.WriteNumber(kv.Key, kv.Value);
        w.WriteEndObject();
        if (c.Fuzzy.HasValue)
            w.WriteNumber("fuzzy", c.Fuzzy.Value);
        w.WriteEndObject();

        w.WriteEndObject();
    }

    private static void WriteSourcesArray(Utf8JsonWriter w, SourceRef src)
    {
        w.WritePropertyName("sources");
        w.WriteStartArray();
        w.WriteStartObject();
        w.WriteString("abbr", src.Abbr);
        w.WriteString("name", src.Name);
        w.WriteEndObject();
        w.WriteEndArray();
    }

    private static void WriteCommonDocStart(Utf8JsonWriter w, string category, BaseEntity e, string descriptionHtml, SourceRef src)
    {
        w.WriteStartObject();
        // Indexed fields first
        w.WriteString("name", e.Name);
        w.WriteString("description", descriptionHtml);
        // Stored fields
        w.WriteString("slug", e.Slug);
        w.WriteString("category", category);
        WriteSourcesArray(w, src);
    }

    private void WriteSpellDoc(Utf8JsonWriter w, string category, SpellDto s)
    {
        var md = s.Effect is null ? string.Empty : string.Join("\n\n", s.Effect);
        var html = _markdown.ToHtml(md);
        WriteCommonDocStart(w, category, s, html, s.Src);
        w.WriteNumber("circle", s.Circle);
        w.WriteBoolean("isRitual", s.IsRitual);
        w.WriteString("school", s.School);
        w.WriteEndObject();
    }

    private void WriteTalentDoc(Utf8JsonWriter w, string category, TalentDto t)
    {
        var extra = (t.Benefits is { Count: > 0 }) ? ("\n\n" + string.Join("\n", t.Benefits.Select(b => "* " + b))) : string.Empty;
        var html = _markdown.ToHtml((t.Description ?? string.Empty) + extra);
        WriteCommonDocStart(w, category, t, html, t.Src);
        w.WriteString("type", t.Category);
        w.WriteEndObject();
    }

    private void WriteBackgroundDoc(Utf8JsonWriter w, string category, BackgroundDto b)
    {
        var html = _markdown.ToHtml(b.Description ?? string.Empty);
        WriteCommonDocStart(w, category, b, html, b.Src);
        w.WriteEndObject();
    }

    private void WriteClassDoc(Utf8JsonWriter w, string category, ClassDto c)
    {
        var html = _markdown.ToHtml(c.Description ?? string.Empty);
        WriteCommonDocStart(w, category, c, html, c.Src);
        w.WriteEndObject();
    }

    private void WriteLineageDoc(Utf8JsonWriter w, string category, LineageDto l)
    {
        var md = (l.Traits is null) ? string.Empty : string.Join("\n\n", l.Traits.Select(t => $"#### {t.Name}\n\n{t.Description}"));
        var html = _markdown.ToHtml(md);
        WriteCommonDocStart(w, category, l, html, l.Src);
        w.WriteEndObject();
    }
}
