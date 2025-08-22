using System.Text.Json;
using BfSiteGen.Core.IO;
using BfSiteGen.Core.Models;

namespace BfSiteGen.Core.Publishing;

public sealed class IndexBuilder
{
    public sealed class IndexConfig
    {
        public string[] Fields { get; init; } = new[] { "name", "descriptionHtml" };
        public Dictionary<string, double> Boost { get; init; } = new() { { "name", 3.0 }, { "descriptionHtml", 1.0 } };
        public double? Fuzzy { get; init; } = 0.2; // MiniSearch fuzzy ratio
        public string[] StoreFields { get; init; } = new[] { "slug", "category", "sources", "circle", "school", "isRitual", "type" };
    }

    private readonly IndexConfig _config;

    public IndexBuilder(IndexConfig? config = null)
    {
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
        using var w = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false });
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

    private static void WriteSourcesArray(Utf8JsonWriter w, List<SourceRef> sources)
    {
        w.WritePropertyName("sources");
        w.WriteStartArray();
        foreach (var s in sources.OrderBy(x => x.Abbr, StringComparer.Ordinal).ThenBy(x => x.Name, StringComparer.Ordinal))
        {
            w.WriteStartObject();
            w.WriteString("abbr", s.Abbr);
            w.WriteString("name", s.Name);
            w.WriteEndObject();
        }
        w.WriteEndArray();
    }

    private static void WriteCommonDocStart(Utf8JsonWriter w, string category, EntryBase e)
    {
        w.WriteStartObject();
        // Indexed fields first
        w.WriteString("name", e.Name);
        w.WriteString("descriptionHtml", e.DescriptionHtml);
        // Stored fields
        w.WriteString("slug", e.Slug);
        w.WriteString("category", category);
        WriteSourcesArray(w, e.Sources);
    }

    private static void WriteSpellDoc(Utf8JsonWriter w, string category, Spell s)
    {
        WriteCommonDocStart(w, category, s);
        w.WriteNumber("circle", s.Circle);
        w.WriteString("circleType", s.CircleType);
        w.WriteString("school", s.School);
        w.WriteBoolean("isRitual", s.IsRitual);
        w.WriteEndObject();
    }

    private static void WriteTalentDoc(Utf8JsonWriter w, string category, Talent t)
    {
        WriteCommonDocStart(w, category, t);
        w.WriteString("type", t.Type);
        w.WriteEndObject();
    }

    private static void WriteBackgroundDoc(Utf8JsonWriter w, string category, Background b)
    {
        WriteCommonDocStart(w, category, b);
        w.WriteEndObject();
    }

    private static void WriteClassDoc(Utf8JsonWriter w, string category, Class c)
    {
        WriteCommonDocStart(w, category, c);
        w.WriteEndObject();
    }

    private static void WriteLineageDoc(Utf8JsonWriter w, string category, Lineage l)
    {
        WriteCommonDocStart(w, category, l);
        w.WriteEndObject();
    }
}

