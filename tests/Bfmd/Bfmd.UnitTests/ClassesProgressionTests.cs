using Bfmd.Core.Config;
using Bfmd.Core.Services;
using Bfmd.Extractors;

namespace Bfmd.UnitTests;

public class ClassesProgressionTests
{
    private static (string content, Markdig.Syntax.MarkdownDocument doc, MappingConfig map) Load(string file)
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "input", "bfrd", "classes", file);
        if (!File.Exists(path))
        {
            path = Path.Combine(root, "input", "classes", file);
        }
        if (!File.Exists(path))
        {
            var candidates = Directory.EnumerateFiles(Path.Combine(root, "input"), file, SearchOption.AllDirectories)
                .Where(p => p.Contains(Path.Combine("classes", file), StringComparison.OrdinalIgnoreCase))
                .ToList();
            path = candidates.FirstOrDefault() ?? path;
        }
        var content = File.ReadAllText(path);
        var doc = MarkdownAst.Parse(content);
        var map = new YamlLoader<MappingConfig>().Load(Path.Combine(root, "config", "mapping.classes.yaml"));
        return (content, doc, map);
    }

    [Fact]
    public void ClassesExtractor_ShouldPopulateRogueFeatures_WhenParsingProgressionTable()
    {
        var (content, doc, map) = Load("rogue.md");
        var (title, header) = ClassesExtractor.GetHeadingTextAndNode(doc, map.EntryHeaderLevel);
        Assert.Equal("ПЛУТ", title);
        var blocks = ClassesExtractor.GetSectionBlocks(doc, header);
        var features = ClassesExtractor.ParseProgressionFeatures(blocks);
        Assert.True(features.Count > 0);
        Assert.True(features.ContainsKey(1));
        Assert.Contains("Компетентность (2)", features[1]);
        Assert.Contains("Скрытая атака", features[1]);
        Assert.Contains("Воровской жаргон", features[1]);
        Assert.Contains("Улучшение", features[4]);
        Assert.Contains("Героический дар", features[10]);
    }

    [Fact]
    public void ClassesExtractor_ShouldPopulateWizardFeatures_WhenParsingProgressionTable()
    {
        var (content, doc, map) = Load("wizard.md");
        var (title, header) = ClassesExtractor.GetHeadingTextAndNode(doc, map.EntryHeaderLevel);
        Assert.Equal("ВОЛШЕБНИК", title);
        var blocks = ClassesExtractor.GetSectionBlocks(doc, header);
        var features = ClassesExtractor.ParseProgressionFeatures(blocks);
        Assert.True(features.Count > 0);
        Assert.Contains("Тайное Восстановление", features[1]);
        Assert.Contains("Использование Заклинаний", features[1]);
        Assert.Contains("Умение Подкласса", features[11]);
        Assert.Contains(features[20], s => s.Equals("Эпический Дар", StringComparison.OrdinalIgnoreCase));
    }

    private static string FindRepoRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "BfTools.sln"))) return dir;
            dir = Path.GetDirectoryName(dir)!;
        }
        throw new DirectoryNotFoundException("Could not locate repo root");
    }
}
