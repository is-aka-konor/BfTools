namespace Bfmd.Cli.Services;

public static class ConfigTemplates
{
    public static string SourcesYaml() =>
        """
sources:
  - abbr: BFRD
    name: Best Fantasy Rulebook
    version: "1.0"
    url: https://example.com
    license: CC-BY
    inputRoot: input
""";

    public static string PipelineYaml() =>
        """
steps:
  - type: classes
    input: input/classes
    mapping: mapping.classes.yaml
    outputData: output/data/classes
    outputIndex: output/index/classes.index.json
    enabled: true
  - type: backgrounds
    input: input/backgrounds
    mapping: mapping.backgrounds.yaml
    outputData: output/data/backgrounds
    outputIndex: output/index/backgrounds.index.json
    enabled: true
  - type: lineages
    input: input/lineages
    mapping: mapping.lineages.yaml
    outputData: output/data/lineages
    outputIndex: output/index/lineages.index.json
    enabled: true
""";

    public static string MappingYaml() =>
        """
titleHeaders: ["# "]
summaryHeaders: ["Описание", "Сводка"]
featuresHeaders: ["Черты", "Особенности"]
subclassesHeaders: ["Подклассы"]
columnMap: { level: "Уровень", proficiencyBonus: "Бонус мастерства", features: "Особенности" }
skillsHeaders: ["Навыки"]
toolsHeaders: ["Инструменты"]
languagesHeaders: ["Языки"]
equipmentHeaders: ["Снаряжение"]
talentHeaders: ["Таланты"]
sizeHeaders: ["Размер"]
speedHeaders: ["Скорость"]
traitsHeaders: ["Черты"]
heritagesHeaders: ["Наследия"]
synonyms: { }
regexes: { LevelCapture: "^Уровень\\s+(\\d+)" }
unitsSpeed: "ft"
""";
}

