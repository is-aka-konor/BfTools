namespace BfCommon.Domain.Models;

public class SpellDto : BaseEntity
{
    public int Circle { get; set; }
    public string School { get; set; } = string.Empty;
    public string CastingTime { get; set; } = string.Empty;
    public string Range { get; set; } = string.Empty;
    public string Components { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public List<string> Circles { get; set; } = new();
    public List<string> Effect { get; set; } = new();
}

