using Bfmd.Core.Config;
using Bfmd.Core.Services;
using Bfmd.Extractors;

namespace Bfmd.UnitTests;

public class LineagesExtractorTests
{
    private static (string content, Markdig.Syntax.MarkdownDocument doc, MappingConfig map, string repo) Load()
    {
        var repo = FindRepoRoot();
        var path = Path.Combine(repo, "input", "lineages", "lineage.md");
        var content = File.ReadAllText(path);
        var doc = MarkdownAst.Parse(content);
        var map = new YamlLoader<MappingConfig>().Load(Path.Combine(repo, "config", "mapping.lineages.yaml"));
        return (content, doc, map, repo);
    }

    [Fact]
    public void LineagesExtractor_ShouldParseBasicFields_WhenGivenLineagesMarkdown()
    {
        var (content, doc, map, repo) = Load();
        var list = LineagesExtractor.ParseLineages(doc, content, map, Path.Combine(repo, "input", "lineages", "lineage.md")).ToList();
        Assert.True(list.Count >= 4);

        var dwarf = list.First(l => l.Name.Contains("ДВОРФ", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Средний", dwarf.Size);
        Assert.Equal(30, dwarf.Speed);
        Assert.Contains(dwarf.Traits, t => t.Name.Equals("Темное Зрение", StringComparison.OrdinalIgnoreCase));

        var zver = list.First(l => l.Name.Contains("ЗВЕРОЛЮД", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(zver.Traits, t => t.Name.Equals("Возраст", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(zver.Traits, t => t.Name.Equals("Размер", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(zver.Traits, t => t.Name.Equals("Скорость", StringComparison.OrdinalIgnoreCase));
        var adapt = zver.Traits.First(t => t.Name.Contains("Адаптация", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Птичьи", adapt.Description);
    }

    [Fact]
    public void LineagesExtractor_ShouldPopulateDescription_WithFullMarkdown()
    {
        var (content, doc, map, repo) = Load();
        var list = LineagesExtractor.ParseLineages(doc, content, map, Path.Combine(repo, "input", "lineages", "lineage.md")).ToList();
        Assert.True(list.Count > 0);
        foreach (var lin in list)
        {
            Assert.False(string.IsNullOrWhiteSpace(lin.Description));
            Assert.Equal(content, lin.Description);
        }
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
