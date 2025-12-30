namespace BfCommon.Domain.Models;

public class SubclassDto : BaseEntity
{
    public string ParentClassSlug { get; set; } = string.Empty;
    public List<FeatureDto> Features { get; set; } = new();
    public List<FeatureDto> ProgressionInfo { get; set; } = new();
}