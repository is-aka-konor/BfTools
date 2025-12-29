namespace BfCommon.Domain.Models;

public class SubclassDto : BaseEntity
{
    public string ParentClassSlug { get; set; } = string.Empty;
    public List<SubclassFeatureDto> Features { get; set; } = new();
}

public class SubclassFeatureDto
{
    public int Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
