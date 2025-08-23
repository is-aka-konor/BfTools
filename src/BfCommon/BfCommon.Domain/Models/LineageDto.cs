namespace BfCommon.Domain.Models;

public class LineageDto : BaseEntity
{
    public string Size { get; set; } = string.Empty;
    public int Speed { get; set; }
    public List<TraitDto> Traits { get; set; } = new();
}

public class TraitDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

