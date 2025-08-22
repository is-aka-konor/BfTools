namespace BfSiteGen.Core.Models;

public sealed class Talent : EntryBase
{
    // Expected values: "Magical" | "Martial"
    public string Type { get; init; } = string.Empty;
}

