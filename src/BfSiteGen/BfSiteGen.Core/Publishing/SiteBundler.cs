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
    private Dictionary<string, List<SubclassDto>> _subclassesByClass = new(StringComparer.OrdinalIgnoreCase);

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

        // Subclasses
        var subclassesSorted = load.Subclasses.OrderBy(s => s.Slug, StringComparer.Ordinal).ToList();
        _subclassesByClass = subclassesSorted
            .GroupBy(s => s.ParentClassSlug, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var (subclassesHash, subclassesCount) = WriteCategory(dataDir, "subclasses", subclassesSorted, WriteSubclass);
        categories["subclasses"] = (subclassesHash, subclassesCount);

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
            allSources.AddRange(subclassesSorted.Select(s => s.Src));
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
    private static void WriteSources(Utf8JsonWriter w, SourceRef src)
    {
        w.WritePropertyName("sources");
        w.WriteStartArray();
        w.WriteStartObject();
        w.WriteString("abbr", src.Abbr);
        w.WriteString("name", src.Name);
        w.WriteEndObject();
        w.WriteEndArray();
    }

    private (string hash, int count) WriteCategory<T>(string dataDir, string category, IReadOnlyList<T> items, Action<Utf8JsonWriter, T> writeItem)
        where T : BaseEntity
    {
        Directory.CreateDirectory(dataDir);
        var tmp = Path.Combine(dataDir, $"{category}-tmp-{Guid.NewGuid():N}.json");

        using (var fs = File.Create(tmp))
        using (var w = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = false, SkipValidation = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
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
        // Alphabetical order of properties
        w.WriteString("castingTime", s.CastingTime);
        w.WriteNumber("circle", s.Circle);
        if (s.Circles is { Count: > 0 }) { w.WritePropertyName("circles"); w.WriteStartArray(); foreach (var c in s.Circles) w.WriteStringValue(c); w.WriteEndArray(); }
        w.WriteString("components", s.Components);
        w.WriteString("description", s.ToHtml(r));
        w.WriteString("duration", s.Duration);
        if (s.Effect is { Count: > 0 }) { w.WritePropertyName("effect"); w.WriteStartArray(); foreach (var e in s.Effect) w.WriteStringValue(e); w.WriteEndArray(); }
        w.WriteBoolean("isRitual", s.IsRitual);
        w.WriteString("name", s.Name);
        w.WriteString("range", s.Range);
        w.WriteString("school", s.School);
        w.WriteString("slug", s.Slug);
        WriteSources(w, s.Src);
        w.WriteEndObject();
    }

    private void WriteTalent(Utf8JsonWriter w, TalentDto t)
    {
        var r = new MarkdownRenderer();
        w.WriteStartObject();
        if (t.Benefits is { Count: > 0 }) { w.WritePropertyName("benefits"); w.WriteStartArray(); foreach (var b in t.Benefits) w.WriteStringValue(b); w.WriteEndArray(); }
        if (!string.IsNullOrWhiteSpace(t.Category)) w.WriteString("category", t.Category);
        w.WriteString("description", t.ToHtml(r));
        w.WriteString("name", t.Name);
        if (!string.IsNullOrWhiteSpace(t.Requirement)) w.WriteString("requirement", t.Requirement);
        w.WriteString("slug", t.Slug);
        WriteSources(w, t.Src);
        w.WriteEndObject();
    }

    private void WriteBackground(Utf8JsonWriter w, BackgroundDto b)
    {
        var r = new MarkdownRenderer();
        w.WriteStartObject();
        if (b.Additional is { Count: > 0 }) { w.WritePropertyName("additional"); w.WriteStartArray(); foreach (var a in b.Additional) w.WriteStringValue(a); w.WriteEndArray(); }
        w.WriteString("description", b.ToHtml(r));
        if (b.Equipment is { Count: > 0 }) { w.WritePropertyName("equipment"); w.WriteStartArray(); foreach (var a in b.Equipment) w.WriteStringValue(a); w.WriteEndArray(); }
        // Languages object (granted/from)
        w.WritePropertyName("languages");
        w.WriteStartObject();
        if (b.Languages.Granted is { Count: > 0 }) { w.WritePropertyName("granted"); w.WriteStartArray(); foreach (var g in b.Languages.Granted) w.WriteStringValue(g); w.WriteEndArray(); }
        if (b.Languages.From is { Count: > 0 }) { w.WritePropertyName("from"); w.WriteStartArray(); foreach (var g in b.Languages.From) w.WriteStringValue(g); w.WriteEndArray(); }
        if (b.Languages.Choose.HasValue) w.WriteNumber("choose", b.Languages.Choose.Value);
        w.WriteEndObject();
        w.WriteString("name", b.Name);
        // Skills object
        w.WritePropertyName("skillProficiencies");
        w.WriteStartObject();
        if (b.SkillProficiencies.Granted is { Count: > 0 }) { w.WritePropertyName("granted"); w.WriteStartArray(); foreach (var g in b.SkillProficiencies.Granted) w.WriteStringValue(g); w.WriteEndArray(); }
        if (b.SkillProficiencies.From is { Count: > 0 }) { w.WritePropertyName("from"); w.WriteStartArray(); foreach (var g in b.SkillProficiencies.From) w.WriteStringValue(g); w.WriteEndArray(); }
        if (b.SkillProficiencies.Choose.HasValue) w.WriteNumber("choose", b.SkillProficiencies.Choose.Value);
        w.WriteEndObject();
        w.WriteString("slug", b.Slug);
        WriteSources(w, b.Src);
        // TalentDescription rendered to HTML
        if (!string.IsNullOrWhiteSpace(b.TalentDescription)) w.WriteString("talentDescription", r.RenderBlock(b.TalentDescription));
        // Talent options
        w.WritePropertyName("talentOptions");
        w.WriteStartObject();
        w.WriteNumber("choose", b.TalentOptions.Choose);
        w.WritePropertyName("from"); w.WriteStartArray(); foreach (var f in b.TalentOptions.From) w.WriteStringValue(f); w.WriteEndArray();
        w.WriteEndObject();
        // toolProficiencies
        if (b.ToolProficiencies is not null)
        {
            w.WritePropertyName("toolProficiencies");
            w.WriteStartObject();
            if (b.ToolProficiencies.Granted is { Count: > 0 }) { w.WritePropertyName("granted"); w.WriteStartArray(); foreach (var g in b.ToolProficiencies.Granted) w.WriteStringValue(g); w.WriteEndArray(); }
            if (b.ToolProficiencies.From is { Count: > 0 }) { w.WritePropertyName("from"); w.WriteStartArray(); foreach (var g in b.ToolProficiencies.From) w.WriteStringValue(g); w.WriteEndArray(); }
            if (b.ToolProficiencies.Choose.HasValue) w.WriteNumber("choose", b.ToolProficiencies.Choose.Value);
            w.WriteEndObject();
        }
        w.WriteEndObject();
    }

    private void WriteClass(Utf8JsonWriter w, ClassDto c)
    {
        var r = new MarkdownRenderer();
        w.WriteStartObject();
        // Alphabetical properties
        w.WriteString("description", c.ToHtml(r));
        if (c.Features is { Count: > 0 }) { w.WritePropertyName("features"); w.WriteStartArray(); foreach (var f in c.Features) w.WriteStringValue(f); w.WriteEndArray(); }
        w.WriteString("hitDie", c.HitDie);
        if (c.Levels is { Count: > 0 })
        {
            w.WritePropertyName("levels");
            w.WriteStartArray();
            foreach (var lv in c.Levels)
            {
                w.WriteStartObject();
                w.WriteNumber("level", lv.Level);
                w.WriteString("proficiencyBonus", lv.ProficiencyBonus);
                if (lv.SpellSlots is { Count: > 0 })
                {
                    w.WritePropertyName("slots");
                    w.WriteStartObject();
                    foreach (var kv in lv.SpellSlots.OrderBy(k => k.Key)) w.WriteNumber(kv.Key.ToString(), kv.Value);
                    w.WriteEndObject();
                }
                if (lv.Features is { Count: > 0 }) { w.WritePropertyName("features"); w.WriteStartArray(); foreach (var f in lv.Features) w.WriteStringValue(f); w.WriteEndArray(); }
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        w.WriteString("name", c.Name);
        if (c.PrimaryAbilities is { Count: > 0 }) { w.WritePropertyName("primaryAbilities"); w.WriteStartArray(); foreach (var a in c.PrimaryAbilities) w.WriteStringValue(a); w.WriteEndArray(); }
        // Proficiencies
        w.WritePropertyName("proficiencies");
        w.WriteStartObject();
        w.WritePropertyName("armor"); w.WriteStartArray(); foreach (var a in c.Proficiencies.Armor) w.WriteStringValue(a); w.WriteEndArray();
        w.WritePropertyName("skills");
        w.WriteStartObject();
        if (c.Proficiencies.Skills.Granted is { Count: > 0 }) { w.WritePropertyName("granted"); w.WriteStartArray(); foreach (var g in c.Proficiencies.Skills.Granted) w.WriteStringValue(g); w.WriteEndArray(); }
        if (c.Proficiencies.Skills.From is { Count: > 0 }) { w.WritePropertyName("from"); w.WriteStartArray(); foreach (var g in c.Proficiencies.Skills.From) w.WriteStringValue(g); w.WriteEndArray(); }
        if (c.Proficiencies.Skills.Choose.HasValue) w.WriteNumber("choose", c.Proficiencies.Skills.Choose.Value);
        w.WriteEndObject();
        w.WritePropertyName("tools"); w.WriteStartArray(); foreach (var a in c.Proficiencies.Tools) w.WriteStringValue(a); w.WriteEndArray();
        w.WritePropertyName("weapons"); w.WriteStartArray(); foreach (var a in c.Proficiencies.Weapons) w.WriteStringValue(a); w.WriteEndArray();
        w.WriteEndObject();
        // Saving throws
        if (c.SavingThrows is { Count: > 0 }) { w.WritePropertyName("savingThrows"); w.WriteStartArray(); foreach (var s in c.SavingThrows) w.WriteStringValue(s); w.WriteEndArray(); }
        w.WriteString("slug", c.Slug);
        WriteSources(w, c.Src);
        // startingEquipment as array
        if (c.StartingEquipment.Items is { Count: > 0 }) { w.WritePropertyName("startingEquipment"); w.WriteStartArray(); foreach (var it in c.StartingEquipment.Items) w.WriteStringValue(it); w.WriteEndArray(); }
        var subclasses = c.Subclasses is { Count: > 0 }
            ? c.Subclasses
            : (_subclassesByClass.TryGetValue(c.Slug, out var grouped) ? grouped : null);
        if (subclasses is { Count: > 0 })
        {
            w.WritePropertyName("subclasses");
            w.WriteStartArray();
            foreach (var s in subclasses) WriteSubclassObject(w, s, r);
            w.WriteEndArray();
        }
        w.WriteEndObject();
    }

    private void WriteSubclass(Utf8JsonWriter w, SubclassDto s)
    {
        var r = new MarkdownRenderer();
        WriteSubclassObject(w, s, r);
    }

    private static void WriteSubclassObject(Utf8JsonWriter w, SubclassDto s, MarkdownRenderer r)
    {
        w.WriteStartObject();
        w.WriteString("description", s.ToHtml(r));
        if (s.Features is { Count: > 0 })
        {
            w.WritePropertyName("features");
            w.WriteStartArray();
            foreach (var f in s.Features.OrderBy(l => l.Level).ThenBy(l => l.Name, StringComparer.Ordinal))
            {
                w.WriteStartObject();
                if (!string.IsNullOrWhiteSpace(f.Description)) w.WriteString("description", r.RenderBlock(f.Description));
                w.WriteNumber("level", f.Level);
                w.WriteString("name", f.Name);
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        w.WriteString("name", s.Name);
        w.WriteString("parentClassSlug", s.ParentClassSlug);
        w.WriteString("slug", s.Slug);
        WriteSources(w, s.Src);
        w.WriteEndObject();
    }

    private void WriteLineage(Utf8JsonWriter w, LineageDto l)
    {
        var r = new MarkdownRenderer();
        w.WriteStartObject();
        w.WriteString("description", l.ToHtml(r));
        w.WriteString("name", l.Name);
        w.WriteString("size", l.Size);
        w.WriteString("slug", l.Slug);
        WriteSources(w, l.Src);
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
        w.WriteEndObject();
    }
}
