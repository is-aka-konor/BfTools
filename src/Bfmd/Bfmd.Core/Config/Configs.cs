namespace Bfmd.Core.Config;

public class SourcesConfig
{
    public List<SourceItem> Sources { get; set; } = [];
}

public class SourceItem
{
    public string Abbr { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? License { get; set; }
    public string InputRoot { get; set; } = string.Empty;
}

public class PipelineConfig
{
    public List<PipelineStepConfig> Steps { get; set; } = [];
}

public class PipelineStepConfig
{
    public string Type { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string Mapping { get; set; } = string.Empty;
    public string OutputData { get; set; } = string.Empty;
    public string OutputIndex { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

public class MappingConfig
{
    public int EntryHeaderLevel { get; set; } = 3;
    public List<string> EntrySectionHeaders { get; set; } = new() { "ПРЕДЫСТОРИИ" };
    public List<string> TitleHeaders { get; set; } = [];
    public List<string> SummaryHeaders { get; set; } = [];
    public List<string> FeaturesHeaders { get; set; } = [];
    public List<string> SubclassesHeaders { get; set; } = [];
    public Dictionary<string, string> ColumnMap { get; set; } = new();
    public List<string> SkillsHeaders { get; set; } = [];
    public List<string> ToolsHeaders { get; set; } = [];
    public List<string> LanguagesHeaders { get; set; } = [];
    public List<string> EquipmentHeaders { get; set; } = [];
    public List<string> TalentHeaders { get; set; } = [];
    public List<string> HitsHeaders { get; set; } = [];
    public List<string> ProficienciesHeaders { get; set; } = [];
    public List<string> StartingEquipmentHeaders { get; set; } = [];
    public List<string> SizeHeaders { get; set; } = [];
    public List<string> SpeedHeaders { get; set; } = [];
    public List<string> TraitsHeaders { get; set; } = [];
    public List<string> HeritagesHeaders { get; set; } = [];
    public Dictionary<string, string> Synonyms { get; set; } = new();
    public Dictionary<string, string> Regexes { get; set; } = new();
    public string UnitsSpeed { get; set; } = "ft";
}
