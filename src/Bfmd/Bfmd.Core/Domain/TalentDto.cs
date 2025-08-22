namespace Bfmd.Core.Domain;

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
    /// Full list of parsed bullet-point features (advantages, mechanics).
    /// </summary>
    public List<string> Benefits { get; init; } = new();

    /// <summary>
    /// Full raw markdown block of the talent (for rendering in the UI).
    /// </summary>
    public string Description { get; init; } = string.Empty;
}
