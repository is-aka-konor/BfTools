namespace BfCommon.Domain.Models;

public class BaseEntity
{
    /// <summary>
    /// Full raw markdown block of the talent (for rendering in the UI).
    /// </summary>
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = "1.0.0";
    public SourceRef Src { get; set; } = new();
    public string? Summary { get; set; }
    public string? SourceFile { get; set; }
}