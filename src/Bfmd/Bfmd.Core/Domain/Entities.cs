using System.Text.Json.Serialization;

namespace Bfmd.Core.Domain;

public class SourceRef
{
    public string Abbr { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? License { get; set; }
    public string Hash { get; set; } = string.Empty;
}

public abstract class BaseEntity
{
    public string SchemaVersion { get; set; } = "1.0.0";
    public string Type { get; set; } = string.Empty; // class|background|lineage
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Aliases { get; set; } = [];
    public string Lang { get; set; } = "ru-RU";
    public SourceRef Src { get; set; } = new();
    public string? Summary { get; set; }
    public string? Text { get; set; }
    public List<string> Tags { get; set; } = [];
    [JsonIgnore]
    public string? SourceFile { get; set; }
}

public class ProficienciesDto
{
    public List<string> Armor { get; set; } = [];
    public List<string> Weapons { get; set; } = [];
    public List<string> Tools { get; set; } = [];
    public SkillsPickDto Skills { get; set; } = new();
}

public class SkillsPickDto
{
    public int? Choose { get; set; }
    public List<string> From { get; set; } = [];
    public List<string> Granted { get; set; } = [];
}

public class StartingEquipmentDto
{
    public List<string> Modes { get; set; } = [];
    public List<List<string>> StandardChoices { get; set; } = [];
    public string? Funds { get; set; }
}

public class LevelRowDto
{
    public int Level { get; set; }
    public string ProficiencyBonus { get; set; } = string.Empty;
    public List<string> Features { get; set; } = [];
    public Dictionary<int, int>? SpellSlots { get; set; }
}

public class SubfeatureDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class FeatureDto
{
    public string Name { get; set; } = string.Empty;
    public int? Level { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<SubfeatureDto>? Subfeatures { get; set; }
}

public class SubclassDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public List<FeatureDto> Features { get; set; } = [];
}

public class ClassDto : BaseEntity
{
    public string HitDie { get; set; } = string.Empty; // d6|d8|d10|d12
    public List<string> PrimaryAbilities { get; set; } = [];
    public List<string> SavingThrows { get; set; } = [];
    public ProficienciesDto Proficiencies { get; set; } = new();
    public StartingEquipmentDto StartingEquipment { get; set; } = new();
    public List<LevelRowDto> Levels { get; set; } = [];
    public List<FeatureDto> Features { get; set; } = [];
    public List<SubclassDto> Subclasses { get; set; } = [];
}

public class TalentOptionsDto
{
    public int Choose { get; set; }
    public List<string> From { get; set; } = [];
}

public class BackgroundDto : BaseEntity
{
    public string Concept { get; set; } = string.Empty;
    public SkillsPickDto SkillProficiencies { get; set; } = new();
    public SkillsPickDto ToolProficiencies { get; set; } = new();
    public SkillsPickDto Languages { get; set; } = new();
    public List<string> Equipment { get; set; } = [];
    public TalentOptionsDto TalentOptions { get; set; } = new();
    public Dictionary<string, List<string>> Tables { get; set; } = new();
}

public class AbilityBonusDto
{
    public string Ability { get; set; } = string.Empty;
    public int Value { get; set; }
    public bool? Flexible { get; set; }
}

public class TraitDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class HeritageDto
{
    public string Name { get; set; } = string.Empty;
    public List<TraitDto> Traits { get; set; } = [];
}

public class LineageDto : BaseEntity
{
    public string Size { get; set; } = string.Empty;
    public int Speed { get; set; }
    public List<AbilityBonusDto>? AbilityBonuses { get; set; }
    public List<TraitDto> Traits { get; set; } = [];
    public List<HeritageDto>? Heritages { get; set; }
}
