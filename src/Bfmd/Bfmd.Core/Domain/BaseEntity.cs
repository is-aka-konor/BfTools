namespace Bfmd.Core.Domain;

public class BaseEntity
{
    public string Type { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = "1.0.0";
    public SourceRef Src { get; set; } = new();
    public string? Summary { get; set; }
    public string? SourceFile { get; set; }
}

public class SourceRef
{
    public string Abbr { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? License { get; set; }
    public string Hash { get; set; } = string.Empty;
}

