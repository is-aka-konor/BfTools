using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BfSiteGen.Core.Models;

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

    public static void WriteCanonicalSpell(Utf8JsonWriter w, Spell s)
    {
        // Properties written in lexicographic order of property names
        w.WriteStartObject();
        w.WriteNumber("circle", s.Circle);
        w.WriteString("circleType", s.CircleType);
        w.WriteString("descriptionHtml", s.DescriptionHtml);
        w.WriteString("descriptionMd", s.DescriptionMd);
        w.WriteBoolean("isRitual", s.IsRitual);
        w.WriteString("name", s.Name);
        w.WriteString("school", s.School);
        w.WriteString("slug", s.Slug);
        WriteSources(w, s.Sources);
        w.WriteEndObject();
    }

    public static void WriteCanonicalTalent(Utf8JsonWriter w, Talent t)
    {
        w.WriteStartObject();
        w.WriteString("descriptionHtml", t.DescriptionHtml);
        w.WriteString("descriptionMd", t.DescriptionMd);
        w.WriteString("name", t.Name);
        w.WriteString("slug", t.Slug);
        WriteSources(w, t.Sources);
        w.WriteString("type", t.Type);
        w.WriteEndObject();
    }

    public static void WriteCanonicalBackground(Utf8JsonWriter w, Background b)
    {
        w.WriteStartObject();
        w.WriteString("descriptionHtml", b.DescriptionHtml);
        w.WriteString("descriptionMd", b.DescriptionMd);
        w.WriteString("name", b.Name);
        w.WriteString("slug", b.Slug);
        WriteSources(w, b.Sources);
        w.WriteEndObject();
    }

    public static void WriteCanonicalClass(Utf8JsonWriter w, Class c)
    {
        w.WriteStartObject();
        w.WriteString("descriptionHtml", c.DescriptionHtml);
        w.WriteString("descriptionMd", c.DescriptionMd);
        w.WriteString("name", c.Name);
        w.WriteString("slug", c.Slug);
        WriteSources(w, c.Sources);
        w.WriteEndObject();
    }

    public static void WriteCanonicalLineage(Utf8JsonWriter w, Lineage l)
    {
        w.WriteStartObject();
        w.WriteString("descriptionHtml", l.DescriptionHtml);
        w.WriteString("descriptionMd", l.DescriptionMd);
        w.WriteString("name", l.Name);
        w.WriteString("slug", l.Slug);
        WriteSources(w, l.Sources);
        w.WriteEndObject();
    }

    private static void WriteSources(Utf8JsonWriter w, List<SourceRef> sources)
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
