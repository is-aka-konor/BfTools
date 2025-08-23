using BfCommon.Domain.Models;

namespace Bfmd.Core.Validation;

public static class BaseEntityValidator
{
    public static void Validate(BaseEntity e, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(e.Id)) result.Add("id", "required");
        if (string.IsNullOrWhiteSpace(e.Slug)) result.Add("slug", "required");
        if (string.IsNullOrWhiteSpace(e.Name)) result.Add("name", "required");
        if (string.IsNullOrWhiteSpace(e.Type)) result.Add("type", "required");
        if (string.IsNullOrWhiteSpace(e.SchemaVersion)) result.Add("schemaVersion", "required");
        SourceRefValidator.Validate(e.Src, result);
    }
}
