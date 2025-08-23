using System.Text.Json;
using BfCommon.Domain.Models;
using BfSiteGen.Core.Validation;

namespace BfSiteGen.Core.IO;

public interface IContentReader
{
    ContentLoadResult LoadAll(string outputRoot);
}

public sealed class ContentReader : IContentReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ContentLoadResult LoadAll(string outputRoot)
    {
        var result = new ContentLoadResult();
        var dataRoot = Path.Combine(outputRoot, "data");

        if (!Directory.Exists(dataRoot))
        {
            result.Errors.Add(new ValidationError("root", string.Empty, string.Empty, "dataRoot", $"Data root not found at '{dataRoot}'."));
            return result;
        }

        LoadMany(Path.Combine(dataRoot, "talents"), result, Deserialize<TalentDto>, result.Talents, "talents");
        LoadMany(Path.Combine(dataRoot, "spells"), result, Deserialize<SpellDto>, result.Spells, "spells");
        LoadMany(Path.Combine(dataRoot, "backgrounds"), result, Deserialize<BackgroundDto>, result.Backgrounds, "backgrounds");
        LoadMany(Path.Combine(dataRoot, "classes"), result, Deserialize<ClassDto>, result.Classes, "classes");
        LoadMany(Path.Combine(dataRoot, "lineages"), result, Deserialize<LineageDto>, result.Lineages, "lineages");

        return result;
    }

    private static T? Deserialize<T>(string path)
        => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);

    private static void LoadMany<T>(string dir, ContentLoadResult result, Func<string, T?> factory, List<T> target, string category)
        where T : BaseEntity
    {
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                var item = factory(file);
                if (item is null)
                {
                    result.Errors.Add(new ValidationError(category, file, string.Empty, "json", "Failed to deserialize DTO."));
                    continue;
                }

                ValidateBasic(category, file, item, result);
                target.Add(item);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ValidationError(category, file, string.Empty, "json", $"Failed to parse JSON: {ex.Message}"));
            }
        }
    }

    private static void ValidateBasic(string category, string file, BaseEntity entity, ContentLoadResult result)
    {
        if (string.IsNullOrWhiteSpace(entity.Slug))
            result.Errors.Add(new ValidationError(category, file, entity.Slug, "slug", "Missing required field 'slug'."));
        if (string.IsNullOrWhiteSpace(entity.Name))
            result.Errors.Add(new ValidationError(category, file, entity.Slug, "name", "Missing required field 'name'."));
        if (entity.Src is null)
        {
            result.Errors.Add(new ValidationError(category, file, entity.Slug, "src", "Missing required field 'src'."));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(entity.Src.Abbr))
                result.Errors.Add(new ValidationError(category, file, entity.Slug, "src.abbr", "Missing source 'abbr'."));
            if (string.IsNullOrWhiteSpace(entity.Src.Name))
                result.Errors.Add(new ValidationError(category, file, entity.Slug, "src.name", "Missing source 'name'."));
        }
    }
}
