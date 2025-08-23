namespace BfCommon.Domain.Models;

public class BackgroundDto : BaseEntity
{
    public string? Description { get; set; }
    public SkillsPickDto SkillProficiencies { get; set; } = new();
    public SkillsPickDto ToolProficiencies { get; set; } = new();
    public SkillsPickDto Languages { get; set; } = new();
    public List<string> Equipment { get; set; } = new();
    public List<string> Additional { get; set; } = new();
    public TalentOptionsDto TalentOptions { get; set; } = new();
    public string? TalentDescription { get; set; }
    public string? Concept { get; set; }
}

public class SkillsPickDto
{
    public List<string> Granted { get; set; } = new();
    public int? Choose { get; set; }
    public List<string> From { get; set; } = new();
}

public class TalentOptionsDto
{
    public int Choose { get; set; }
    public List<string> From { get; set; } = new();
}

