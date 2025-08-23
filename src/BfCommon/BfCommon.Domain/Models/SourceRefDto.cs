namespace BfCommon.Domain.Models;

public class SourceRef
{
    public string Abbr { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? License { get; set; }
    public string Hash { get; set; } = string.Empty;
}