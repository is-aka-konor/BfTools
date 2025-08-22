using System.Text;
using System.Text.RegularExpressions;
using Bfmd.Core.Config;
using Bfmd.Core.Domain;
using Bfmd.Core.Pipeline;
using Markdig.Syntax;

namespace Bfmd.Extractors;

public class LineagesExtractor : IExtractor
{
    public IEnumerable<BaseEntity> Extract(IEnumerable<(string path, string content, MarkdownDocument doc, string sha256)> docs, SourceItem src, MappingConfig map)
    {
        foreach (var (path, content, doc, _) in docs)
        {
            foreach (var lin in ParseLineages(doc, content, map, path))
                yield return lin;
        }
    }

    internal static IEnumerable<LineageDto> ParseLineages(MarkdownDocument doc, string content, MappingConfig map, string sourcePath)
    {
        // Optional narrowing: look for a collection root H1/H2 that contains key phrase
        var allH2 = doc.Descendants().OfType<HeadingBlock>().Where(h => h.Level == 2).ToList();
        for (int i = 0; i < allH2.Count; i++)
        {
            var h2 = allH2[i];
            var title = InlineToText(h2.Inline);
            var name = TryParseEntryName(title);
            if (string.IsNullOrWhiteSpace(name)) continue;

            var next = i + 1 < allH2.Count ? allH2[i + 1] : null;
            var blocks = CollectUntil(doc, h2, next).ToList();

            var lin = new LineageDto
            {
                Type = "lineage",
                Name = name!,
                Size = string.Empty,
                Speed = 0,
                Traits = new List<TraitDto>(),
                SourceFile = sourcePath
            };

            ParseBulletsIntoLineage(blocks, lin, map);

            // Ensure requireds
            if (string.IsNullOrWhiteSpace(lin.Size)) lin.Size = "Средний"; // safe default
            if (lin.Speed <= 0) lin.Speed = 30;
            if (lin.Traits.Count == 0)
                lin.Traits.Add(new TraitDto { Name = "Черта", Description = "Описание отсутствует" });

            yield return lin;
        }
    }

    internal static void ParseBulletsIntoLineage(IEnumerable<Block> blocks, LineageDto lin, MappingConfig map)
    {
        foreach (var b in blocks)
        {
            if (b is not ListBlock list) continue;
            foreach (var item in list)
            {
                if (item is not ListItemBlock li) continue;
                var (label, desc) = ExtractLabeledValue(li);
                if (string.IsNullOrWhiteSpace(label) && string.IsNullOrWhiteSpace(desc)) continue;
                var labelNorm = label.Trim().TrimEnd('.');

                if (labelNorm.Equals("Возраст", StringComparison.OrdinalIgnoreCase))
                {
                    lin.Traits.Add(new TraitDto { Name = "Возраст", Description = desc });
                }
                else if (labelNorm.Equals("Размер", StringComparison.OrdinalIgnoreCase))
                {
                    lin.Traits.Add(new TraitDto { Name = "Размер", Description = desc });
                    // Try to set top-level Size to first recognizable option
                    var size = ParseSize(desc);
                    if (!string.IsNullOrWhiteSpace(size)) lin.Size = size;
                }
                else if (labelNorm.Equals("Скорость", StringComparison.OrdinalIgnoreCase))
                {
                    lin.Traits.Add(new TraitDto { Name = "Скорость", Description = desc });
                    var sp = ParseSpeed(desc, map);
                    if (sp > 0) lin.Speed = sp;
                }
                else if (labelNorm.Equals("Темное Зрение", StringComparison.OrdinalIgnoreCase) || labelNorm.Equals("Тёмное Зрение", StringComparison.OrdinalIgnoreCase))
                {
                    lin.Traits.Add(new TraitDto { Name = "Темное Зрение", Description = desc });
                }
                else
                {
                    // Generic trait (may contain nested subtraits)
                    var fullDesc = desc;
                    var nested = li.Descendants().OfType<ListBlock>().FirstOrDefault();
                    if (nested != null)
                    {
                        var parts = new List<string>();
                        foreach (var sub in nested)
                        {
                            if (sub is not ListItemBlock sli) continue;
                            var (nlab, ndesc) = ExtractLabeledValue(sli);
                            if (!string.IsNullOrWhiteSpace(nlab))
                                parts.Add($"{nlab} {ndesc}".Trim());
                            else if (!string.IsNullOrWhiteSpace(ndesc))
                                parts.Add(ndesc);
                        }
                        if (parts.Count > 0)
                        {
                            fullDesc = string.IsNullOrWhiteSpace(fullDesc) ? string.Join(" \n", parts) : fullDesc + "\n" + string.Join(" \n", parts);
                        }
                    }
                    lin.Traits.Add(new TraitDto { Name = labelNorm, Description = fullDesc });
                }
            }
        }
    }

    internal static string ParseSize(string text)
    {
        // Prefer "Средний" or "Маленький" if present; if both, choose "Средний"
        if (Regex.IsMatch(text, "(?i)Средний")) return "Средний";
        if (Regex.IsMatch(text, "(?i)Маленький")) return "Маленький";
        return string.Empty;
    }

    internal static int ParseSpeed(string text, MappingConfig map)
    {
        var rx = map.Regexes != null && map.Regexes.TryGetValue("speedCapture", out var s) ? s : "(?i)(\\d+)";
        var m = Regex.Match(text, rx);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var v)) return v;
        return 0;
    }

    internal static (string label, string value) ExtractLabeledValue(ListItemBlock li)
    {
        var p = li.Descendants().OfType<ParagraphBlock>().FirstOrDefault();
        if (p == null || p.Inline == null) return (string.Empty, string.Empty);
        var label = string.Empty;
        var sb = new StringBuilder();
        foreach (var node in p.Inline)
        {
            if (node is Markdig.Syntax.Inlines.EmphasisInline { DelimiterCount: >= 2 } emph && string.IsNullOrEmpty(label))
            {
                label = InlineToText(emph);
                continue;
            }
            sb.Append(InlineNodeToText(node));
        }
        var value = sb.ToString().Trim().TrimStart(':', '—', '-', ' ');
        return (label.Trim().TrimEnd('.'), value);
    }

    internal static IEnumerable<Block> CollectUntil(MarkdownDocument doc, Block start, Block? nextH2)
    {
        var list = new List<Block>();
        var after = false;
        foreach (var b in doc)
        {
            if (!after)
            {
                if (ReferenceEquals(b, start)) after = true;
                continue;
            }
            if (nextH2 != null && ReferenceEquals(b, nextH2)) break;
            if (b is ThematicBreakBlock) break;
            list.Add(b);
        }
        return list;
    }

    internal static string? TryParseEntryName(string headingText)
    {
        var m = Regex.Match(headingText ?? string.Empty, "(?i)^черт[аы]\\s+происхождени[ея]\\s+(.+)$");
        if (m.Success) return m.Groups[1].Value.Trim();
        return null;
    }

    internal static string InlineToText(Markdig.Syntax.Inlines.ContainerInline? inline)
    {
        if (inline is null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var child in inline) sb.Append(InlineNodeToText(child));
        return sb.ToString().Trim();
    }

    internal static string InlineNodeToText(Markdig.Syntax.Inlines.Inline node)
    {
        return node switch
        {
            Markdig.Syntax.Inlines.LiteralInline lit => lit.Content.ToString(),
            Markdig.Syntax.Inlines.LinkInline link => InlineToText(link),
            Markdig.Syntax.Inlines.EmphasisInline emph => InlineToText(emph),
            Markdig.Syntax.Inlines.CodeInline code => code.Content,
            _ => string.Empty
        };
    }
}
