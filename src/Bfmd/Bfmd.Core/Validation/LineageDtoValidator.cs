using Bfmd.Core.Domain;

namespace Bfmd.Core.Validation;

public class LineageDtoValidator
{
    public ValidationResult Validate(LineageDto l)
    {
        var r = new ValidationResult();
        BaseEntityValidator.Validate(l, r);
        if (string.IsNullOrWhiteSpace(l.Size)) r.Add("size", "required");
        if (l.Speed <= 0) r.Add("speed", "> 0");
        if (l.Traits == null || l.Traits.Count == 0) r.Add("traits", "at least one trait");
        return r;
    }
}

