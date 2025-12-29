using Bfmd.Core.Config;
using Bfmd.Core.Services;
using Bfmd.Extractors;
using Markdig.Syntax;

namespace Bfmd.UnitTests;

public class SubclassesExtractorTests
{
    [Fact]
    public void SubclassesExtractor_ShouldParseHeaderSlugAndFeatures()
    {
        var md = """
        # ЖРЕЦ | CLERIC

        ### ПОДКЛАСС: ДОМЕН ЖИЗНИ | LIFE DOMAIN
        Текст описания подкласса.

        #### РАЗВИТИЕ ДОМЕНА ЖИЗНИ | LIFE DOMAIN PROGRESSION
        | УРОВЕНЬ ЖРЕЦА | УМЕНИЯ |
        |---|---|
        | 3-й | Божественный канал |

        #### БОЖЕСТВЕННЫЙ КАНАЛ | CHANNEL DIVINITY
        *Умение Домена Жизни 3-го уровня*
        Описание умения.
        """;

        var doc = MarkdownAst.Parse(md);
        var map = new MappingConfig { EntryHeaderLevel = 2 };
        var src = new SourceItem { Abbr = "SRC", Name = "Source", Version = "1.0", InputRoot = "input/tovpg1" };
        var ex = new SubclassesExtractor();

        var res = ex.Extract([(path: "input/tovpg1/subclasses/cleric.md", content: md, doc, sha256: "abc")], src, map)
            .OfType<BfCommon.Domain.Models.SubclassDto>()
            .ToList();

        Assert.Single(res);
        var sub = res[0];
        Assert.Equal("ДОМЕН ЖИЗНИ", sub.Name);
        Assert.Equal("life-domain", sub.Slug);
        Assert.Equal("cleric", sub.ParentClassSlug);
        Assert.True(sub.Features.Count >= 1);
        Assert.Contains(sub.Features, f => f.Level == 3 && f.Name.Contains("БОЖЕСТВЕННЫЙ КАНАЛ", StringComparison.OrdinalIgnoreCase));
        Assert.True(sub.ProgressionInfo.Count >= 1);
        Assert.Contains(sub.ProgressionInfo, f => f.Level == null && f.Name.Contains("РАЗВИТИЕ", StringComparison.OrdinalIgnoreCase));
    }
}
