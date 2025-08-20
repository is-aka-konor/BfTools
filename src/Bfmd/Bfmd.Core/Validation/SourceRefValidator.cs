using Bfmd.Core.Domain;

namespace Bfmd.Core.Validation;

public static class SourceRefValidator
{
    public static void Validate(SourceRef src, ValidationResult result, string prefix = "src")
    {
        if (string.IsNullOrWhiteSpace(src.Abbr)) result.Add(prefix + ".abbr", "required");
        if (string.IsNullOrWhiteSpace(src.Name)) result.Add(prefix + ".name", "required");
        if (string.IsNullOrWhiteSpace(src.Version)) result.Add(prefix + ".version", "required");
        if (string.IsNullOrWhiteSpace(src.Hash)) result.Add(prefix + ".hash", "required");
    }
}
