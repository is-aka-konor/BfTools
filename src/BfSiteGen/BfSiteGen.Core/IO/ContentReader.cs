using System.Text.Json;
using BfSiteGen.Core.Models;
using BfSiteGen.Core.Validation;
using BfSiteGen.Core.Services;

namespace BfSiteGen.Core.IO;

public interface IContentReader
{
    ContentLoadResult LoadAll(string outputRoot);
}

public sealed class ContentReader : IContentReader
{
    private readonly IMarkdownRenderer _markdown;

    public ContentReader(IMarkdownRenderer markdown)
    {
        _markdown = markdown;
    }

    public ContentLoadResult LoadAll(string outputRoot)
    {
        var result = new ContentLoadResult();
        var dataRoot = Path.Combine(outputRoot, "data");

        if (!Directory.Exists(dataRoot))
        {
            result.Errors.Add(new ValidationError("root", string.Empty, string.Empty, "dataRoot", $"Data root not found at '{dataRoot}'."));
            return result;
        }

        LoadTalents(Path.Combine(dataRoot, "talents"), result);
        LoadSpells(Path.Combine(dataRoot, "spells"), result);
        LoadBackgrounds(Path.Combine(dataRoot, "backgrounds"), result);
        LoadClasses(Path.Combine(dataRoot, "classes"), result);
        LoadLineages(Path.Combine(dataRoot, "lineages"), result);

        return result;
    }

    private void LoadTalents(string dir, ContentLoadResult result)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                using var stream = File.OpenRead(file);
                using var doc = JsonDocument.Parse(stream);
                var root = doc.RootElement;
                
                var talent = new Talent
                {
                    Slug = GetString(root, "slug") ?? string.Empty,
                    Name = GetString(root, "name") ?? string.Empty,
                    DescriptionMd = GetString(root, "descriptionMd") ?? GetString(root, "description") ?? string.Empty,
                    Type = GetString(root, "type") ?? string.Empty,
                    Sources = ReadSources(root)
                };

                ValidateCommon("talents", file, talent, result);
                // Specific: type must be Magical or Martial
                if (string.IsNullOrWhiteSpace(talent.Type))
                {
                    result.Errors.Add(new ValidationError("talents", file, talent.Slug, "type", "Missing required field 'type' (Magical|Martial)."));
                }
                else if (!string.Equals(talent.Type, "Magical", StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(talent.Type, "Martial", StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add(new ValidationError("talents", file, talent.Slug, "type", "Invalid 'type'. Expected 'Magical' or 'Martial'."));
                }

                talent.DescriptionHtml = _markdown.ToHtml(talent.DescriptionMd);
                result.Talents.Add(talent);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ValidationError("talents", file, string.Empty, "json", $"Failed to parse JSON: {ex.Message}"));
            }
        }
    }

    private void LoadSpells(string dir, ContentLoadResult result)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                using var stream = File.OpenRead(file);
                using var doc = JsonDocument.Parse(stream);
                var root = doc.RootElement;

                var spell = new Spell
                {
                    Slug = GetString(root, "slug") ?? string.Empty,
                    Name = GetString(root, "name") ?? string.Empty,
                    DescriptionMd = GetString(root, "descriptionMd") ?? GetString(root, "description") ?? string.Empty,
                    Circle = GetInt(root, "circle"),
                    School = GetString(root, "school") ?? string.Empty,
                    IsRitual = GetBool(root, "isRitual"),
                    CircleType = GetString(root, "circleType") ?? string.Empty,
                    Sources = ReadSources(root)
                };

                ValidateCommon("spells", file, spell, result);
                if (spell.Circle == int.MinValue)
                    result.Errors.Add(new ValidationError("spells", file, spell.Slug, "circle", "Missing required field 'circle' (int)."));
                if (string.IsNullOrWhiteSpace(spell.School))
                    result.Errors.Add(new ValidationError("spells", file, spell.Slug, "school", "Missing required field 'school' (string)."));
                if (string.IsNullOrWhiteSpace(spell.CircleType))
                    result.Errors.Add(new ValidationError("spells", file, spell.Slug, "circleType", "Missing required field 'circleType' (string)."));

                spell.DescriptionHtml = _markdown.ToHtml(spell.DescriptionMd);
                result.Spells.Add(spell);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ValidationError("spells", file, string.Empty, "json", $"Failed to parse JSON: {ex.Message}"));
            }
        }
    }

    private void LoadBackgrounds(string dir, ContentLoadResult result)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                using var stream = File.OpenRead(file);
                using var doc = JsonDocument.Parse(stream);
                var root = doc.RootElement;

                var item = new Background
                {
                    Slug = GetString(root, "slug") ?? string.Empty,
                    Name = GetString(root, "name") ?? string.Empty,
                    DescriptionMd = GetString(root, "descriptionMd") ?? GetString(root, "description") ?? string.Empty,
                    Sources = ReadSources(root)
                };

                ValidateCommon("backgrounds", file, item, result);
                item.DescriptionHtml = _markdown.ToHtml(item.DescriptionMd);
                result.Backgrounds.Add(item);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ValidationError("backgrounds", file, string.Empty, "json", $"Failed to parse JSON: {ex.Message}"));
            }
        }
    }

    private void LoadClasses(string dir, ContentLoadResult result)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                using var stream = File.OpenRead(file);
                using var doc = JsonDocument.Parse(stream);
                var root = doc.RootElement;

                var item = new Class
                {
                    Slug = GetString(root, "slug") ?? string.Empty,
                    Name = GetString(root, "name") ?? string.Empty,
                    DescriptionMd = GetString(root, "descriptionMd") ?? GetString(root, "description") ?? string.Empty,
                    Sources = ReadSources(root)
                };

                ValidateCommon("classes", file, item, result);
                item.DescriptionHtml = _markdown.ToHtml(item.DescriptionMd);
                result.Classes.Add(item);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ValidationError("classes", file, string.Empty, "json", $"Failed to parse JSON: {ex.Message}"));
            }
        }
    }

    private void LoadLineages(string dir, ContentLoadResult result)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                using var stream = File.OpenRead(file);
                using var doc = JsonDocument.Parse(stream);
                var root = doc.RootElement;

                var item = new Lineage
                {
                    Slug = GetString(root, "slug") ?? string.Empty,
                    Name = GetString(root, "name") ?? string.Empty,
                    DescriptionMd = GetString(root, "descriptionMd") ?? GetString(root, "description") ?? string.Empty,
                    Sources = ReadSources(root)
                };

                ValidateCommon("lineages", file, item, result);
                item.DescriptionHtml = _markdown.ToHtml(item.DescriptionMd);
                result.Lineages.Add(item);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ValidationError("lineages", file, string.Empty, "json", $"Failed to parse JSON: {ex.Message}"));
            }
        }
    }

    private static void ValidateCommon(string category, string file, EntryBase entry, ContentLoadResult result)
    {
        if (string.IsNullOrWhiteSpace(entry.Slug))
            result.Errors.Add(new ValidationError(category, file, entry.Slug, "slug", "Missing required field 'slug'."));
        if (string.IsNullOrWhiteSpace(entry.Name))
            result.Errors.Add(new ValidationError(category, file, entry.Slug, "name", "Missing required field 'name'."));
        if (string.IsNullOrWhiteSpace(entry.DescriptionMd))
            result.Errors.Add(new ValidationError(category, file, entry.Slug, "descriptionMd", "Missing required field 'descriptionMd'."));
        if (entry.Sources.Count == 0)
            result.Errors.Add(new ValidationError(category, file, entry.Slug, "sources", "Missing required field 'sources' with at least one item."));
        else
        {
            for (var i = 0; i < entry.Sources.Count; i++)
            {
                var s = entry.Sources[i];
                if (string.IsNullOrWhiteSpace(s.Abbr))
                    result.Errors.Add(new ValidationError(category, file, entry.Slug, $"sources[{i}].abbr", "Missing source 'abbr'."));
                if (string.IsNullOrWhiteSpace(s.Name))
                    result.Errors.Add(new ValidationError(category, file, entry.Slug, $"sources[{i}].name", "Missing source 'name'."));
            }
        }
    }

    private static string? GetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private static int GetInt(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v)
            ? v
            : int.MinValue;

    private static bool GetBool(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.True;

    private static List<SourceRef> ReadSources(JsonElement root)
    {
        var list = new List<SourceRef>();
        if (root.TryGetProperty("sources", out var sEl) && sEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in sEl.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    var abbr = item.TryGetProperty("abbr", out var a) && a.ValueKind == JsonValueKind.String ? a.GetString() ?? string.Empty : string.Empty;
                    var name = item.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() ?? string.Empty : string.Empty;
                    list.Add(new SourceRef { Abbr = abbr, Name = name });
                }
            }
        }
        else if (root.TryGetProperty("src", out var src) && src.ValueKind == JsonValueKind.Object)
        {
            var abbr = src.TryGetProperty("abbr", out var a) && a.ValueKind == JsonValueKind.String ? a.GetString() ?? string.Empty : string.Empty;
            var name = src.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() ?? string.Empty : string.Empty;
            list.Add(new SourceRef { Abbr = abbr, Name = name });
        }
        return list;
    }
}
