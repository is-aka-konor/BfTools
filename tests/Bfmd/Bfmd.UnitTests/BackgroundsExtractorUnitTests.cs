using System.Linq;
using Bfmd.Core.Config;
using Bfmd.Core.Services;
using Bfmd.Extractors;
using Markdig;
using Markdig.Syntax;

namespace Bfmd.UnitTests;

public class BackgroundsExtractorUnitTests
{
    private static (string content, MarkdownDocument doc, MappingConfig map) LoadBackgroundsDoc()
    {
        var repo = FindRepoRoot();
        var path = Path.Combine(repo, "input", "backgrounds", "backgrounds.md");
        var content = File.ReadAllText(path);
        var doc = MarkdownAst.Parse(content);
        var map = new YamlLoader<MappingConfig>().Load(Path.Combine(repo, "config", "mapping.backgrounds.yaml"));
        return (content, doc, map);
    }

    [Fact]
    public void FindSection_ShouldFindBackgroundsSection_WhenUsingMappingHeaders()
    {
        var (_, doc, map) = LoadBackgroundsDoc();
        var (start, end) = BackgroundsExtractor.FindSection(doc, 2, map.EntrySectionHeaders);
        Assert.NotNull(start);
        Assert.Equal(2, start!.Level);
    }

    [Fact]
    public void NextParagraph_ShouldFindFirstParagraph_WhenAfterHeader()
    {
        var (_, doc, _) = LoadBackgroundsDoc();
        var h4 = doc.Descendants().OfType<HeadingBlock>().First(h => h.Level == 4);
        var para = BackgroundsExtractor.NextParagraph(h4);
        Assert.NotNull(para);
        Assert.IsType<ParagraphBlock>(para);
    }

    [Theory]
    [InlineData("*   **Владение Навыками:** Выберите два из: Скрытность, Расследование, Проницательность или Обман.", 2, new[] { "Скрытность", "Расследование", "Проницательность", "Обман" })]
    [InlineData("*   **Владение Навыками:** Выберите два из: Магия, История, Природа или Религия.", 2, new[] { "Магия", "История", "Природа", "Религия" })]
    public void ParseSkillsPick_ShouldParseChooseAndOptions_WhenGivenListItem(string md, int choose, string[] expected)
    {
        var doc = Markdown.Parse(md);
        var li = doc.Descendants().OfType<ListItemBlock>().First();
        var text = BackgroundsExtractor.FlattenText(li);
        var sp = BackgroundsExtractor.ParseSkillsPick(text);
        Assert.Equal(choose, sp.Choose);
        foreach (var item in expected) Assert.Contains(item, sp.From);
    }

    [Theory]
    [InlineData("*   **Снаряжение:** Пять кусков мела, крюк-кошка, комплект темной одежды путешественника или костюм, и мешочек с 10 зм.",
        new[] { "Пять кусков мела", "крюк-кошка", "комплект темной одежды путешественника", "костюм", "мешочек с 10 зм" })]
    public void ParseEquipment_ShouldSplitItems_WhenGivenEquipmentLine(string md, string[] expected)
    {
        var doc = Markdown.Parse(md);
        var text = BackgroundsExtractor.FlattenText(doc.Descendants().OfType<ListItemBlock>().First());
        var items = BackgroundsExtractor.ParseEquipment(text);
        foreach (var e in expected) Assert.Contains(e, items);
    }

    [Theory]
    [InlineData("*   **Дополнительные Владения:** Вы знаете Воровской Жаргон. Если вы уже знаете этот язык, вы изучаете другой язык по вашему выбору. Получите владение одним инструментом и одним транспортным средством.",
        "Жаргон")]
    public void ParseAdditional_ShouldContainKeyPhrase_WhenParsingAdditionalLine(string md, string mustContain)
    {
        var doc = Markdown.Parse(md);
        var text = BackgroundsExtractor.FlattenText(doc.Descendants().OfType<ListItemBlock>().First());
        var items = BackgroundsExtractor.ParseAdditional(text);
        Assert.True(items.Count >= 1);
        Assert.Contains(mustContain, string.Join(" ", items));
    }

    [Fact]
    public void ParseTalentOptions_ShouldExtractOptions_WhenGivenTalentParagraph()
    {
        var paragraph = "Вы сводили концы с концами на задворках законопослушного общества. Выберите талант из этого списка, чтобы отразить ваш опыт: Скрытный, Дотошный или Касание Удачи.";
        var opt = BackgroundsExtractor.ParseTalentOptions(paragraph);
        Assert.Equal(1, opt.Choose);
        Assert.Contains("Скрытный", opt.From);
        Assert.Contains("Дотошный", opt.From);
        Assert.Contains("Касание Удачи", opt.From);
    }

    [Fact]
    public void SplitItems_ShouldNormalizeConjunctions_WhenSplittingList()
    {
        var list = BackgroundsExtractor.SplitItems("А, Б или В и Г.");
        Assert.Equal(new[] { "А", "Б", "В", "Г" }, list);
    }

    [Fact]
    public void BlocksToMarkdown_ShouldReturnSlice_WhenGivenBlocksRange()
    {
        var (content, doc, map) = LoadBackgroundsDoc();
        var (sectionStart, sectionEnd) = BackgroundsExtractor.FindSection(doc, 2, map.EntrySectionHeaders);
        var entries = doc.Descendants().OfType<HeadingBlock>()
            .Where(h => h.Level == map.EntryHeaderLevel)
            .Where(h => sectionStart == null || (h.Span.Start > sectionStart.Span.Start && (sectionEnd == null || h.Span.Start < sectionEnd.Span.Start)))
            .ToList();
        Assert.True(entries.Count > 0);
        var h3 = entries[0];
        var next = entries.Count > 1 ? entries[1] : null;
        var until = BackgroundsExtractor.NextBoundary(doc, h3, next).ToList();
        var md = BackgroundsExtractor.BlocksToMarkdown(content, until);
        Assert.False(string.IsNullOrWhiteSpace(md));
    }

    [Fact]
    public void HeaderMatches_ShouldReturnTrue_WhenHeaderMatchesAnyMapping()
    {
        var (_, doc, map) = LoadBackgroundsDoc();
        var h4 = doc.Descendants().OfType<HeadingBlock>().First(h => h.Level == 4);
        Assert.True(BackgroundsExtractor.HeaderMatches(h4, map.TalentHeaders));
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
