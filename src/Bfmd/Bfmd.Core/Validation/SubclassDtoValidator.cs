using BfCommon.Domain.Models;

namespace Bfmd.Core.Validation;

public class SubclassDtoValidator
{
    public ValidationResult Validate(SubclassDto s)
    {
        var r = new ValidationResult();
        BaseEntityValidator.Validate(s, r);
        if (string.IsNullOrWhiteSpace(s.ParentClassSlug)) r.Add("parentClassSlug", "required");

        foreach (var feature in s.Features)
        {
            if (feature.Level <= 0) r.Add("features", "level must be positive");
            if (string.IsNullOrWhiteSpace(feature.Name)) r.Add("features", "name required");
        }
        return r;
    }
}
