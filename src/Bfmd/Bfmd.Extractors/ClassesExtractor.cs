using System.Text;
using System.Text.RegularExpressions;
using Bfmd.Core.Config;
using Bfmd.Core.Domain;
using Bfmd.Core.Pipeline;
using Bfmd.Core.Services;
using Markdig.Syntax;

namespace Bfmd.Extractors;

public class ClassesExtractor : IExtractor
{
    public IEnumerable<BaseEntity> Extract(IEnumerable<(string path, string content, MarkdownDocument doc, string sha256)> docs, SourceItem src, MappingConfig map)
    {
        foreach (var (path, _, doc, _) in docs)
        {
            var title = GetHeadingText(doc);
            if (string.IsNullOrWhiteSpace(title)) continue;

            var cls = new ClassDto
            {
                Type = "class",
                Name = title!,
                Summary = GetFirstParagraph(doc),
                HitDie = GuessHitDie(doc),
                SavingThrows = ["Constitution", "Strength"],
                Proficiencies = new ProficienciesDto { Skills = new SkillsPickDto { Choose = 0, From = [], Granted = []
                } },
                StartingEquipment = new StartingEquipmentDto(),
                Levels = Enumerable.Range(1, 20).Select(i => new LevelRowDto { Level = i, ProficiencyBonus = $"+{((i - 1) / 4) + 2}", Features =
                    []
                }).ToList(),
                Features = [],
                Subclasses = [],
                SourceFile = path
            };

            yield return cls;
        }
    }

    private static string GetFirstParagraph(MarkdownDocument doc)
    {
        var p = doc.Descendants().OfType<ParagraphBlock>().FirstOrDefault();
        return p is null ? string.Empty : InlineToText(p.Inline);
    }

    private static string GuessHitDie(MarkdownDocument doc)
    {
        var text = string.Join("\n", doc.Descendants().OfType<ParagraphBlock>().Select(p => InlineToText(p.Inline)));
        if (Regex.IsMatch(text, "d12", RegexOptions.IgnoreCase)) return "d12";
        if (Regex.IsMatch(text, "d10", RegexOptions.IgnoreCase)) return "d10";
        if (Regex.IsMatch(text, "d8", RegexOptions.IgnoreCase)) return "d8";
        return "d6";
    }

    private static string GetHeadingText(MarkdownDocument doc)
    {
        var h = doc.Descendants().OfType<HeadingBlock>().FirstOrDefault(h => h.Level is 1 or 2);
        return h is null ? string.Empty : InlineToText(h.Inline);
    }

    private static string InlineToText(Markdig.Syntax.Inlines.ContainerInline? inline)
    {
        if (inline is null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var child in inline)
        {
            switch (child)
            {
                case Markdig.Syntax.Inlines.LiteralInline lit:
                    sb.Append(lit.Content.ToString());
                    break;
                case Markdig.Syntax.Inlines.LinkInline link:
                    sb.Append(InlineToText(link));
                    break;
                case Markdig.Syntax.Inlines.EmphasisInline emph:
                    sb.Append(InlineToText(emph));
                    break;
                case Markdig.Syntax.Inlines.CodeInline code:
                    sb.Append(code.Content.ToString());
                    break;
            }
        }
        return sb.ToString().Trim();
    }
}
