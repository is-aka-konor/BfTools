using Bfmd.Core.Config;
using Bfmd.Core.Services;

namespace Bfmd.UnitTests;

public class ClassesExtractorTests
{
    [Fact]
    public void ClassesExtractor_Parses_Fighter_Proficiencies_And_Skills()
    {
        var repoRoot = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(repoRoot) && !File.Exists(Path.Combine(repoRoot, "BfTools.sln")))
            repoRoot = Path.GetDirectoryName(repoRoot)!;
        var path = Path.Combine(repoRoot, "input", "classes", "fighter.md");
        Assert.True(File.Exists(path));

        var content = File.ReadAllText(path);
        var doc = MarkdownAst.Parse(content);
        var src = new SourceItem { Abbr = "SRC", Name = "Src", Version = "1", InputRoot = Path.Combine(repoRoot, "input") };
        var map = new YamlLoader<MappingConfig>().Load(Path.Combine(repoRoot, "config", "mapping.classes.yaml"));
        var ex = new Bfmd.Extractors.ClassesExtractor();

        var res = ex.Extract([(path: path, content: content, doc, sha256: "abc")], src, map).OfType<Bfmd.Core.Domain.ClassDto>().Single();
        Assert.Equal("ВОИН", res.Name);
        Assert.False(string.IsNullOrWhiteSpace(res.Description));
        Assert.Equal("d10", res.HitDie);

        // Skills: "Выберите два из следующих: ..."
        Assert.Equal(2, res.Proficiencies.Skills.Choose);
        Assert.Contains("Акробатика", res.Proficiencies.Skills.From);
        Assert.Contains("Выживание", res.Proficiencies.Skills.From);

        // Armor/Weapons/Tools
        Assert.Contains(res.Proficiencies.Armor, a => a!.Contains("доспехи", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(res.Proficiencies.Weapons, w => w!.Contains("оружие", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(res.Proficiencies.Tools);
    }
}

