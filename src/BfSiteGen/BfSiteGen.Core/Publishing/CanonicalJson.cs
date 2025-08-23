using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BfCommon.Domain.Models;

namespace BfSiteGen.Core.Publishing;

public static class CanonicalJson
{
    public static byte[] SerializeCanonicalArray<T>(IEnumerable<T> items, Action<Utf8JsonWriter,T> writeItem)
    {
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions
        {
            Indented = false,
            SkipValidation = false
        });
        writer.WriteStartArray();
        // For deterministic hashes across input order, sort by a stable key when available.
        // If T exposes a string property named "Slug", sort lexicographically by it; otherwise preserve order.
        IEnumerable<T> ordered = items;
        try
        {
            var prop = typeof(T).GetProperty("Slug");
            if (prop != null && prop.PropertyType == typeof(string))
            {
                ordered = items.OrderBy(x => (string?)prop.GetValue(x) ?? string.Empty, StringComparer.Ordinal);
            }
        }
        catch
        {
            // ignore and fall back to provided order
        }

        foreach (var item in ordered)
        {
            writeItem(writer, item);
        }
        writer.WriteEndArray();
        writer.Flush();
        return ms.ToArray();
    }

    public static void WriteCanonicalSpell(Utf8JsonWriter w, SpellDto s)
    {
        w.WriteStartObject();
        w.WriteNumber("circle", s.Circle);
        w.WriteString("castingTime", s.CastingTime);
        w.WriteString("components", s.Components);
        if (s.Circles is { Count: > 0 })
        {
            w.WritePropertyName("circles");
            w.WriteStartArray();
            foreach (var c in s.Circles) w.WriteStringValue(c);
            w.WriteEndArray();
        }
        if (s.Effect is { Count: > 0 })
        {
            w.WritePropertyName("effect");
            w.WriteStartArray();
            foreach (var e in s.Effect) w.WriteStringValue(e);
            w.WriteEndArray();
        }
        w.WriteString("duration", s.Duration);
        w.WriteString("name", s.Name);
        w.WriteString("range", s.Range);
        w.WriteString("school", s.School);
        w.WriteString("slug", s.Slug);
        WriteSrc(w, s.Src);
        w.WriteEndObject();
    }

    public static void WriteCanonicalTalent(Utf8JsonWriter w, TalentDto t)
    {
        w.WriteStartObject();
        if (t.Benefits is { Count: > 0 })
        {
            w.WritePropertyName("benefits");
            w.WriteStartArray();
            foreach (var b in t.Benefits) w.WriteStringValue(b);
            w.WriteEndArray();
        }
        w.WriteString("category", t.Category);
        w.WriteString("description", t.Description);
        w.WriteString("name", t.Name);
        w.WriteString("slug", t.Slug);
        w.WriteString("requirement", t.Requirement);
        WriteSrc(w, t.Src);
        w.WriteEndObject();
    }

    public static void WriteCanonicalBackground(Utf8JsonWriter w, BackgroundDto b)
    {
        w.WriteStartObject();
        if (b.Description is not null) w.WriteString("description", b.Description);
        w.WriteString("name", b.Name);
        w.WriteString("slug", b.Slug);
        // skills
        w.WritePropertyName("skillProficiencies");
        WriteSkillsPick(w, b.SkillProficiencies);
        w.WritePropertyName("toolProficiencies");
        WriteSkillsPick(w, b.ToolProficiencies);
        w.WritePropertyName("languages");
        WriteSkillsPick(w, b.Languages);
        if (b.Equipment is { Count: > 0 })
        {
            w.WritePropertyName("equipment");
            w.WriteStartArray();
            foreach (var e in b.Equipment) w.WriteStringValue(e);
            w.WriteEndArray();
        }
        if (b.Additional is { Count: > 0 })
        {
            w.WritePropertyName("additional");
            w.WriteStartArray();
            foreach (var e in b.Additional) w.WriteStringValue(e);
            w.WriteEndArray();
        }
        w.WritePropertyName("talentOptions");
        w.WriteStartObject();
        w.WriteNumber("choose", b.TalentOptions.Choose);
        w.WritePropertyName("from");
        w.WriteStartArray();
        foreach (var f in b.TalentOptions.From) w.WriteStringValue(f);
        w.WriteEndArray();
        w.WriteEndObject();
        if (b.TalentDescription is not null) w.WriteString("talentDescription", b.TalentDescription);
        if (b.Concept is not null) w.WriteString("concept", b.Concept);
        WriteSrc(w, b.Src);
        w.WriteEndObject();
    }

    private static void WriteSkillsPick(Utf8JsonWriter w, SkillsPickDto s)
    {
        w.WriteStartObject();
        w.WritePropertyName("granted");
        w.WriteStartArray(); foreach (var g in s.Granted) w.WriteStringValue(g); w.WriteEndArray();
        if (s.Choose.HasValue) w.WriteNumber("choose", s.Choose.Value);
        w.WritePropertyName("from");
        w.WriteStartArray(); foreach (var g in s.From) w.WriteStringValue(g); w.WriteEndArray();
        w.WriteEndObject();
    }

    public static void WriteCanonicalClass(Utf8JsonWriter w, ClassDto c)
    {
        w.WriteStartObject();
        if (c.Description is not null) w.WriteString("description", c.Description);
        w.WriteString("name", c.Name);
        w.WriteString("slug", c.Slug);
        w.WriteString("hitDie", c.HitDie);
        if (c.SavingThrows is { Count: > 0 })
        {
            w.WritePropertyName("savingThrows");
            w.WriteStartArray();
            foreach (var s in c.SavingThrows) w.WriteStringValue(s);
            w.WriteEndArray();
        }
        if (c.PrimaryAbilities is { Count: > 0 })
        {
            w.WritePropertyName("primaryAbilities");
            w.WriteStartArray();
            foreach (var s in c.PrimaryAbilities) w.WriteStringValue(s);
            w.WriteEndArray();
        }
        w.WritePropertyName("proficiencies");
        w.WriteStartObject();
        w.WritePropertyName("skills");
        WriteSkillsPick(w, c.Proficiencies.Skills);
        w.WritePropertyName("armor");
        w.WriteStartArray(); foreach (var a in c.Proficiencies.Armor) w.WriteStringValue(a); w.WriteEndArray();
        w.WritePropertyName("weapons");
        w.WriteStartArray(); foreach (var a in c.Proficiencies.Weapons) w.WriteStringValue(a); w.WriteEndArray();
        w.WritePropertyName("tools");
        w.WriteStartArray(); foreach (var a in c.Proficiencies.Tools) w.WriteStringValue(a); w.WriteEndArray();
        w.WriteEndObject();
        w.WritePropertyName("startingEquipment");
        w.WriteStartObject();
        w.WritePropertyName("items");
        w.WriteStartArray(); foreach (var it in c.StartingEquipment.Items) w.WriteStringValue(it); w.WriteEndArray();
        w.WriteEndObject();
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
                    w.WritePropertyName("spellSlots");
                    w.WriteStartObject();
                    foreach (var kv in lv.SpellSlots.OrderBy(k => k.Key))
                        w.WriteNumber(kv.Key.ToString(), kv.Value);
                    w.WriteEndObject();
                }
                w.WritePropertyName("features");
                w.WriteStartArray(); foreach (var f in lv.Features) w.WriteStringValue(f); w.WriteEndArray();
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        if (c.Features is { Count: > 0 }) { w.WritePropertyName("features"); w.WriteStartArray(); foreach (var f in c.Features) w.WriteStringValue(f); w.WriteEndArray(); }
        if (c.Subclasses is { Count: > 0 }) { w.WritePropertyName("subclasses"); w.WriteStartArray(); foreach (var f in c.Subclasses) w.WriteStringValue(f); w.WriteEndArray(); }
        WriteSrc(w, c.Src);
        w.WriteEndObject();
    }

    public static void WriteCanonicalLineage(Utf8JsonWriter w, LineageDto l)
    {
        w.WriteStartObject();
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
        w.WriteString("name", l.Name);
        w.WriteString("slug", l.Slug);
        WriteSrc(w, l.Src);
        w.WriteEndObject();
    }

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

    public static string Sha256Hex(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
