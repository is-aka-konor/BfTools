namespace BfSiteGen.Core.Models;

public abstract class EntryBase
{
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DescriptionMd { get; init; } = string.Empty;
    public string DescriptionHtml { get; set; } = string.Empty;
    public List<SourceRef> Sources { get; init; } = new();
}

