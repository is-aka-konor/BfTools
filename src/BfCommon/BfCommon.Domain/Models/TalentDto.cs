namespace BfCommon.Domain.Models;

public class TalentDto : BaseEntity
{
    /// <summary>
    /// Category of the talent (e.g. "Магические Таланты", "Воинские Таланты").
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Requirement text if present, otherwise null.
    /// </summary>
    public string Requirement { get; init; } = string.Empty;

    /// <summary>
    /// Paragraph-to-bullets mapping for structured talent features.
    /// </summary>
    public Dictionary<string, List<string>> TalentFeatures { get; init; } = new(StringComparer.Ordinal);
}
