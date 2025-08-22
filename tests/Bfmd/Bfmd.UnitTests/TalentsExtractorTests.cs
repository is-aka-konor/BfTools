using System.Text;
using System.Text.RegularExpressions;
using Bfmd.Core.Config;
using Bfmd.Extractors;
using Markdig;
using Markdig.Syntax;

namespace Bfmd.UnitTests;

public class TalentsExtractorUnitTests
{
    [Fact]
    public void LooksLikeCategory_ShouldDetect_WhenContainsTalentsWord()
    {
        Assert.True(TalentsExtractor.LooksLikeCategory("МАГИЧЕСКИЕ ТАЛАНТЫ"));
        Assert.True(TalentsExtractor.LooksLikeCategory("Воинские таланты"));
        Assert.False(TalentsExtractor.LooksLikeCategory("Оглавление"));
    }

    [Fact]
    public void NormalizeCategory_ShouldUseSynonyms_WhenProvidedInMapping()
    {
        var map = new MappingConfig { Regexes = new Dictionary<string, string> { ["categorySynonyms.МАГИЧЕСКИЕ ТАЛАНТЫ"] = "Магические" } };
        Assert.Equal("Магические", TalentsExtractor.NormalizeCategory("МАГИЧЕСКИЕ ТАЛАНТЫ", map));
        Assert.Equal("Воинские", TalentsExtractor.NormalizeCategory("ВОИНСКИЕ ТАЛАНТЫ", map));
    }

    [Fact]
    public void ParseRequirementAndBenefits_ShouldExtractRequirementAndBullets()
    {
        var md = string.Join('\n', new[]
        {
            "* **Требование:** ИНТ 13 или выше",
            "* Бонус 1",
            "* Бонус 2"
        });
        var doc = Markdown.Parse(md);
        var blocks = doc.ToList();
        // AST-friendly patterns: paragraph text has no bullet '*' and no emphasis markers
        var rxReq = new Regex(@"требован[её][^:：]*[:：]\s*(.+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var rxBen = new Regex(@"^(?!\s*требован[её][^:：]*[:：]).+\S.*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var (req, bens) = TalentsExtractor.ParseRequirementAndBenefits(blocks, rxReq, rxBen, new List<Regex>());
        Assert.Equal("ИНТ 13 или выше", req);
        Assert.Equal(new[] { "Бонус 1", "Бонус 2" }, bens);
    }

    [Fact]
    public void ExtractRawMarkdown_ShouldSliceFromEntryHeading_ToNextEntryHeading()
    {
        var md = string.Join('\n', new[]
        {
            "## ТАЛАНТЫ",
            "",
            "### МАГИЧЕСКИЕ ТАЛАНТЫ",
            "",
            "#### Арканист",
            "Текст А",
            "",
            "* Пункт",
            "",
            "#### Боевое Колдовство",
            "Текст Б"
        });
        var doc = Markdown.Parse(md);
        var content = md;
        var entry = doc.Descendants().OfType<HeadingBlock>().First(h => h.Level == 4);
        var slice = TalentsExtractor.ExtractRawMarkdown(doc, content, entry);
        Assert.Contains("#### Арканист", slice);
        Assert.Contains("Текст А", slice);
        Assert.DoesNotContain("Боевое Колдовство", slice);
    }

    [Fact]
    public void FindScopeBounds_ShouldStartAtRootHeader_WhenConfigured()
    {
        var md = string.Join('\n', new[]
        {
            "# Документ",
            "",
            "## ТАЛАНТЫ",
            "",
            "### МАГИЧЕСКИЕ ТАЛАНТЫ",
            "#### Арканист"
        });
        var doc = Markdown.Parse(md);
        var blocks = doc.ToList();
        var (start, end) = TalentsExtractor.FindScopeBounds(blocks, new List<string> { "ТАЛАНТЫ" }, 2);
        Assert.True(start >= 0);
        Assert.IsType<HeadingBlock>(blocks[start]);
        Assert.Equal(2, ((HeadingBlock)blocks[start]).Level);
    }
}
