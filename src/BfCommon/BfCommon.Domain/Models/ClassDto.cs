namespace BfCommon.Domain.Models;

public class ClassDto : BaseEntity
{
    public string? Description { get; set; }
    public string HitDie { get; set; } = string.Empty;
    public List<string> SavingThrows { get; set; } = new();
    public List<string>? PrimaryAbilities { get; set; }
    public ProficienciesDto Proficiencies { get; set; } = new();
    public StartingEquipmentDto StartingEquipment { get; set; } = new();
    public List<LevelRowDto> Levels { get; set; } = new();
    public List<string> Features { get; set; } = new();
    public List<string> Subclasses { get; set; } = new();
}

public class ProficienciesDto
{
    public SkillsPickDto Skills { get; set; } = new();
    public List<string> Armor { get; set; } = new();
    public List<string> Weapons { get; set; } = new();
    public List<string> Tools { get; set; } = new();
}

public class StartingEquipmentDto
{
    public List<string> Items { get; set; } = new();
}

public class LevelRowDto
{
    public int Level { get; set; }
    public string ProficiencyBonus { get; set; } = string.Empty;
    public Dictionary<int, int>? SpellSlots { get; set; }
    public List<string> Features { get; set; } = new();
}
