namespace BfSiteGen.Core.Models;

public sealed class Spell : EntryBase
{
    public int Circle { get; init; }
    public string School { get; init; } = string.Empty;
    public bool IsRitual { get; init; }
    public string CircleType { get; init; } = string.Empty;
}

