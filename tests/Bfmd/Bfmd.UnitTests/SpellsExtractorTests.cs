using System.Linq;
using Bfmd.Extractors;
using Markdig;
using Markdig.Syntax;

namespace Bfmd.UnitTests;

public class SpellsExtractorTests
{
    [Theory]
    [InlineData("## Развеивание магии (Dispel Magic)", "Развеивание магии")]
    [InlineData("## Божественное благоволение (Divine Favor)", "Божественное благоволение")]
    public void ParseNameRu_ShouldExtractRussianTitle_WhenHeadingHasEnglishInParentheses(string h2Line, string expected)
    {
        var doc = Markdown.Parse(h2Line);
        var h2 = doc.Descendants().OfType<HeadingBlock>().First();
        var name = SpellsExtractor.ParseNameRu(h2);
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData("3-й круг, Тайное и Божественное (Ограждение)", 3, new[] { "Тайное", "Божественное" }, "Ограждение")]
    [InlineData("1-й круг, Божественное (Воплощение)", 1, new[] { "Божественное" }, "Воплощение")]
    public void ParseLevelLine_ShouldParseCircleTraditionsAndSchool_WhenGivenRussianLine(string input, int expCircle, string[] expTraditions, string expSchool)
    {
        SpellsExtractor.ParseLevelLine(input, out var circle, out var traditions, out var school);
        Assert.Equal(expCircle, circle);
        Assert.Equal(expTraditions, traditions);
        Assert.Equal(expSchool, school);
    }

    [Theory]
    [InlineData("- **Дистанция:** 120 футов", "Дистанция", "120 футов")]
    [InlineData("- **Компоненты:** В, С", "Компоненты", "В, С")]
    public void ExtractLabeledValue_ShouldParseLabelAndValue_WhenGivenLabeledParagraph(string md, string expLabel, string expValue)
    {
        var doc = Markdown.Parse(md);
        var p = doc.Descendants().OfType<ParagraphBlock>().First();
        var (label, value) = SpellsExtractor.ExtractLabeledValue(p);
        Assert.Equal(expLabel.ToLowerInvariant(), SpellsExtractor.NormalizeLabel(label));
        Assert.Contains(expValue, value);
    }

    [Theory]
    [InlineData("Дистанция:", "дистанция")]
    [InlineData("Длительность：", "длительность")]
    public void NormalizeLabel_ShouldReturnLowercaseCore_WhenGivenVariousColons(string input, string expected)
    {
        Assert.Equal(expected, SpellsExtractor.NormalizeLabel(input));
    }

    [Theory]
    [InlineData("Тайное и Божественное", new[] { "Тайное", "Божественное" })]
    [InlineData("Божественное, Потустороннее", new[] { "Божественное", "Потустороннее" })]
    public void SplitList_ShouldSplitAndNormalize_WhenGivenRussianConjunctions(string input, string[] expected)
    {
        Assert.Equal(expected, SpellsExtractor.SplitList(input));
    }

    [Fact]
    public void CollectEffect_ShouldCollectParagraphsAndSubheadings_WhenFollowingEffectListItem()
    {
        var md = string.Join('\n', new[]
        {
            "- **Эффект**: Начальный эффект.",
            "",
            "Абзац после эффекта.",
            "",
            "##### На более высоких кругах.",
            "Когда вы накладываете это заклинание..."
        });
        var doc = Markdown.Parse(md);
        var blocks = doc.ToList();
        var listIndex = blocks.FindIndex(b => b is ListBlock);
        Assert.True(listIndex >= 0);
        var extras = SpellsExtractor.CollectEffect(blocks, listIndex + 1);
        Assert.Equal(3, extras.Count);
        Assert.Contains("Абзац после эффекта.", extras[0]);
        Assert.StartsWith("На более высоких кругах.", extras[1]);
        Assert.StartsWith("Когда вы накладываете", extras[2]);
    }

    [Fact]
    public void ParseSpellBody_ShouldParseDispelMagic_WhenProvidedSectionBlocks()
    {
        var md = string.Join('\n', new[]
        {
            "## Развеивание магии (Dispel Magic)",
            "- **Уровень**: **3-й круг, Тайное и Божественное (Ограждение)** / 3rd-Circle Arcane and Divine (Abjuration)",
            "- **Время накладывания:** 1 действие",
            "- **Дистанция:** 120 футов",
            "- **Компоненты:** В, С",
            "- **Длительность:** Мгновенная",
            "- **Эффект**: Выберите одно существо, предмет или магический эффект в пределах дистанции...",
            "",
            "##### На более высоких кругах.",
            "Когда вы накладываете это заклинание, используя ячейку заклинания 4-го круга..."
        });
        var doc = Markdown.Parse(md);
        var h2s = doc.Descendants().OfType<HeadingBlock>().Where(h => h.Level == 2).ToList();
        var blocks = SpellsExtractor.FindNextBreakOrH2(doc, h2s[0], null);
        var (circle, traditions, school, casting, range, components, duration, effect) = SpellsExtractor.ParseSpellBody(blocks);

        Assert.Equal(3, circle);
        Assert.Equal(new[] { "Тайное", "Божественное" }, traditions);
        Assert.Equal("Ограждение", school);
        Assert.Equal("1 действие", casting);
        Assert.Equal("120 футов", range);
        Assert.Equal("В, С", components);
        Assert.Equal("Мгновенная", duration);
        Assert.True(effect.Count >= 2);
        Assert.StartsWith("Выберите одно существо", effect[0]);
        Assert.StartsWith("На более высоких кругах.", effect[1]);
    }

    [Fact]
    public void ParseSpellBody_ShouldParseDivineFavor_WhenProvidedSectionBlocks()
    {
        var md = string.Join('\n', new[]
        {
            "## Божественное благоволение (Divine Favor)",
            "- **Уровень**: **1-й круг, Божественное (Воплощение)** / 1st-Circle Divine (Evocation)",
            "- **Время накладывания:** 1 бонусное действие",
            "- **Дистанция:** На себя",
            "- **Компоненты:** В, С",
            "- **Длительность:** Концентрация, до 1 минуты",
            "- **Эффект**: Ваша молитва наделяет вас божественным сиянием..."
        });
        var doc = Markdown.Parse(md);
        var h2s = doc.Descendants().OfType<HeadingBlock>().Where(h => h.Level == 2).ToList();
        var blocks = SpellsExtractor.FindNextBreakOrH2(doc, h2s[0], null);
        var (circle, traditions, school, casting, range, components, duration, effect) = SpellsExtractor.ParseSpellBody(blocks);

        Assert.Equal(1, circle);
        Assert.Equal(new[] { "Божественное" }, traditions);
        Assert.Equal("Воплощение", school);
        Assert.Equal("1 бонусное действие", casting);
        Assert.Equal("На себя", range);
        Assert.Equal("В, С", components);
        Assert.Equal("Концентрация, до 1 минуты", duration);
        Assert.True(effect.Count >= 1);
        Assert.StartsWith("Ваша молитва", effect[0]);
    }
}
