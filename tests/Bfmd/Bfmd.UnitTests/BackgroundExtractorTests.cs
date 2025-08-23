using BfCommon.Domain.Models;
using Bfmd.Core.Config;
using Bfmd.Core.Services;

namespace Bfmd.UnitTests;

public class BackgroundExtractorTests
{
    [Fact]
    public void BackgroundsExtractor_ShouldParseFirstEntry_WhenGivenBackgroundsMarkdown()
    {
        var repoRoot = FindRepoRoot();
        var path = Path.Combine(repoRoot, "input", "backgrounds", "backgrounds.md");
        Assert.True(File.Exists(path));
        var content = File.ReadAllText(path);
        var doc = MarkdownAst.Parse(content);
        var src = new Bfmd.Core.Config.SourceItem { Abbr = "BFRD", Name = "Black Flag Reference Document", Version = "1.0", InputRoot = Path.Combine(repoRoot, "input") };
        var map = new YamlLoader<MappingConfig>().Load(Path.Combine(repoRoot, "config", "mapping.backgrounds.yaml"));

        var ex = new Bfmd.Extractors.BackgroundsExtractor();
        var res = ex.Extract([(path: path, content: content, doc, sha256: "deadbeef")], src, map).OfType<BackgroundDto>().ToList();
        Assert.True(res.Count >= 1);
        var first = res[0];
        Assert.Equal("ПРЕСТУПНИК", first.Name);
        Assert.Equal(2, first.SkillProficiencies.Choose);
        Assert.Contains("Скрытность", first.SkillProficiencies.From);
        Assert.Contains("Обман", first.SkillProficiencies.From);
        Assert.True(first.Equipment.Count > 0);
        Assert.False(string.IsNullOrWhiteSpace(first.Description));
        Assert.False(string.IsNullOrWhiteSpace(first.TalentDescription));
        // Additional may be a single merged sentence; ensure it references the jargon
        if (first.Additional.Count > 0)
            Assert.Contains("Жаргон", first.Additional[0]);
        Assert.Contains("Скрытный", first.TalentOptions.From);
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
