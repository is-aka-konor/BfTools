using System.Text;
using System.Text.RegularExpressions;
using Bfmd.Core.Config;
using BfCommon.Domain.Models;
using Bfmd.Core.Pipeline;
using Markdig.Syntax;

namespace Bfmd.Extractors;

public class LineagesExtractor : IExtractor
{
    /// <summary>
    /// Extracts lineage DTOs from each markdown document using the lineage parser.
    /// </summary>
    public IEnumerable<BaseEntity> Extract(IEnumerable<(string path, string content, MarkdownDocument doc, string sha256)> docs, SourceItem src, MappingConfig map)
    {
        foreach (var (path, content, doc, _) in docs)
        {
            foreach (var lin in ParseLineages(doc, content, map, path))
                yield return lin;
        }
    }

    /// <summary>
    /// Parses all H2 lineage sections and builds one DTO per section, using the H2 title for name/slug
    /// and the blocks between the H2 and the next H2 or thematic break as the description/traits source.
    /// </summary>
    internal static IEnumerable<LineageDto> ParseLineages(MarkdownDocument doc, string content, MappingConfig map, string sourcePath)
    {
        var allH2 = doc.Descendants().OfType<HeadingBlock>().Where(h => h.Level == 2).ToList();
        for (int i = 0; i < allH2.Count; i++)
        {
            var h2 = allH2[i];
            var title = InlineToText(h2.Inline);
            if (!TryParseHeaderParts(title, out var name, out var slug)) continue;

            var next = i + 1 < allH2.Count ? allH2[i + 1] : null;
            var blocks = CollectUntil(doc, h2, next).ToList();

            var lin = new LineageDto
            {
                Type = "lineage",
                Name = name,
                Slug = slug,
                Size = string.Empty,
                Speed = 0,
                Traits = new List<TraitDto>(),
                Description = BlocksToMarkdown(content, blocks),
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

    /// <summary>
    /// Walks list blocks inside a lineage section and maps labeled bullets into traits,
    /// also deriving size and speed from matching labels.
    /// </summary>
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

    /// <summary>
    /// Infers the first recognizable size token from the text.
    /// </summary>
    internal static string ParseSize(string text)
    {
        // Prefer "Средний" or "Маленький" if present; if both, choose "Средний"
        if (Regex.IsMatch(text, "(?i)Средний")) return "Средний";
        if (Regex.IsMatch(text, "(?i)Маленький")) return "Маленький";
        return string.Empty;
    }

    /// <summary>
    /// Extracts the first speed number using a configurable capture regex.
    /// </summary>
    internal static int ParseSpeed(string text, MappingConfig map)
    {
        var rx = map.Regexes != null && map.Regexes.TryGetValue("speedCapture", out var s) ? s : "(?i)(\\d+)";
        var m = Regex.Match(text, rx);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var v)) return v;
        return 0;
    }

    /// <summary>
    /// Extracts a labeled bullet like **Label.** value into label/value strings.
    /// </summary>
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

    /// <summary>
    /// Collects blocks after a start heading until the next H2 or a thematic break.
    /// </summary>
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

    /// <summary>
    /// Splits a H2 heading into name and slug using the "Name | SLUG" convention.
    /// </summary>
    internal static bool TryParseHeaderParts(string headingText, out string name, out string slug)
    {
        name = string.Empty;
        slug = string.Empty;
        var text = headingText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var split = text.Split('|', 2, StringSplitOptions.TrimEntries);
        if (split.Length < 2) return false;

        name = split[0].Trim();
        slug = split[1].Trim().ToLower();
        return !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(slug);
    }

    /// <summary>
    /// Flattens inline markup into plain text.
    /// </summary>
    internal static string InlineToText(Markdig.Syntax.Inlines.ContainerInline? inline)
    {
        if (inline is null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var child in inline) sb.Append(InlineNodeToText(child));
        return sb.ToString().Trim();
    }

    /// <summary>
    /// Converts a single inline node to text, traversing known inline types.
    /// </summary>
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

    /// <summary>
    /// Slices the original markdown content for the provided block span range.
    /// </summary>
    internal static string BlocksToMarkdown(string content, List<Block> blocks)
    {
        if (blocks.Count == 0) return string.Empty;
        var start = blocks.First().Span.Start;
        var end = blocks.Last().Span.End;
        if (start < 0 || end < 0 || end < start || end + 1 > content.Length) return string.Empty;
        var slice = content.Substring(start, end - start + 1);
        return slice.Trim();
    }
}
