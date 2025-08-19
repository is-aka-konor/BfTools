using Bfmd.Core.Config;

namespace Bfmd.Core.Validation;

public static class SourceBindingValidation
{
    public static bool AbbrExists(string abbr, SourcesConfig cfg)
        => cfg.Sources.Any(s => string.Equals(s.Abbr, abbr, StringComparison.OrdinalIgnoreCase));
}

