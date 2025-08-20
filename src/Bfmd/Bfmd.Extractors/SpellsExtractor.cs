using System.Text;
using System.Text.RegularExpressions;
using Bfmd.Core.Config;
using Bfmd.Core.Domain;
using Bfmd.Core.Pipeline;
using Markdig.Syntax;

namespace Bfmd.Extractors;

public class SpellsExtractor : IExtractor
{
    public IEnumerable<BaseEntity> Extract(IEnumerable<(string path, string content, MarkdownDocument doc, string sha256)> docs, SourceItem src, MappingConfig map)
    {
        foreach (var (path, _, doc, _) in docs)
        {
            // Only process files starting with spells_
            var file = Path.GetFileName(path);
            if (file is null || !file.StartsWith("spells_", StringComparison.OrdinalIgnoreCase))
                continue;

            var h2s = doc.Descendants().OfType<HeadingBlock>().Where(h => h.Level == 2).ToList();
            for (int i = 0; i < h2s.Count; i++)
            {
                var h2 = h2s[i];
                var blocks = FindNextBreakOrH2(doc, h2, h2s.ElementAtOrDefault(i + 1)).ToList();

                var nameRu = ParseNameRu(h2);
                if (string.IsNullOrWhiteSpace(nameRu)) continue;

                var (circle, traditions, school, casting, range, components, duration, effect) = ParseSpellBody(blocks);
                if (circle <= 0 || effect.Count == 0)
                    continue; // skip malformed entries

                yield return new SpellDto
                {
                    Type = "spell",
                    Name = nameRu,
                    Circle = circle,
                    Circles = traditions,
                    School = school,
                    CastingTime = casting,
                    Range = range,
                    Components = components,
                    Duration = duration,
                    Effect = effect,
                    SourceFile = path
                };
            }
        }
    }

    private static List<Block> FindNextBreakOrH2(MarkdownDocument doc, Block start, HeadingBlock? nextH2)
    {
        var result = new List<Block>();
        bool after = false;
        foreach (var b in doc)
        {
            if (!after)
            {
                if (ReferenceEquals(b, start)) after = true;
                continue;
            }
            if (b is ThematicBreakBlock) break;
            if (nextH2 != null && ReferenceEquals(b, nextH2)) break;
            result.Add(b);
        }
        return result;
    }

    private static string ParseNameRu(HeadingBlock h2)
    {
        var text = InlineToText(h2.Inline);
        var idx = text.IndexOf('(');
        return idx > 0 ? text[..idx].Trim() : text.Trim();
    }

    private static (int circle, List<string> traditions, string school, string casting, string range, string components, string duration, List<string> effect)
        ParseSpellBody(IReadOnlyList<Block> blocks)
    {
        int circle = 0; var traditions = new List<string>(); var school = string.Empty;
        string casting = string.Empty, range = string.Empty, components = string.Empty, duration = string.Empty;
        var effect = new List<string>();

        for (int bi = 0; bi < blocks.Count; bi++)
        {
            var b = blocks[bi];
            if (b is ListBlock list)
            {
                foreach (var item in list)
                {
                    if (item is not ListItemBlock li) continue;
                    var p = li.Descendants().OfType<ParagraphBlock>().FirstOrDefault();
                    if (p is null) continue;
                    var (label, value) = ExtractLabeledValue(p);
                    if (string.IsNullOrWhiteSpace(label)) continue;
                    switch (NormalizeLabel(label))
                    {
                        case "уровень":
                            var ru = value.Split('/')[0].Trim();
                            ParseLevelLine(ru, out circle, out traditions, out school);
                            break;
                        case "время накладывания":
                            casting = value.Trim();
                            break;
                        case "дистанция":
                            range = value.Trim();
                            break;
                        case "компоненты":
                            components = value.Trim();
                            break;
                        case "длительность":
                            duration = value.Trim();
                            break;
                        case "эффект":
                            if (!string.IsNullOrWhiteSpace(value)) effect.Add(value.Trim());
                            // Collect subsequent paras/headings until thematic break or next H2 (bounded by section)
                            for (int fj = bi + 1; fj < blocks.Count; fj++)
                            {
                                var f = blocks[fj];
                                if (f is HeadingBlock { Level: 2 }) break;
                                if (f is ThematicBreakBlock) break;
                                if (f is ParagraphBlock pp)
                                {
                                    var t = InlineToText(pp.Inline);
                                    if (!string.IsNullOrWhiteSpace(t)) effect.Add(t);
                                }
                                else if (f is HeadingBlock sub)
                                {
                                    var ht = InlineToText(sub.Inline);
                                    if (!string.IsNullOrWhiteSpace(ht)) effect.Add(ht);
                                }
                            }
                            break;
                    }
                }
            }
        }

        return (circle, traditions, school, casting, range, components, duration, effect);
    }

    // removed FollowingBlocks to avoid multiple enumeration and improve performance

    private static void ParseLevelLine(string russian, out int circle, out List<string> traditions, out string school)
    {
        circle = 0; traditions = new List<string>(); school = string.Empty;
        var m = Regex.Match(russian, @"(\d+)\s*-?й\s+круг", RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var c)) circle = c;
        // Extract traditions: part between comma after "круг," and the first (
        var commaIdx = russian.IndexOf(',');
        var parIdx = russian.IndexOf('(');
        if (commaIdx >= 0 && parIdx > commaIdx)
        {
            var mid = russian.Substring(commaIdx + 1, parIdx - commaIdx - 1);
            traditions = SplitList(mid);
        }
        // School: text in first parentheses
        var sm = Regex.Match(russian, @"\(([^)]+)\)");
        if (sm.Success) school = sm.Groups[1].Value.Trim();
    }

    private static (string label, string value) ExtractLabeledValue(ParagraphBlock p)
    {
        // Find first strong label
        var label = string.Empty;
        var sb = new StringBuilder();
        if (p.Inline is null) return (string.Empty, string.Empty);
        foreach (var node in p.Inline)
        {
            if (node is Markdig.Syntax.Inlines.EmphasisInline { DelimiterCount: >= 2 } emph && string.IsNullOrEmpty(label))
            {
                label = InlineToText(emph);
                continue;
            }
            sb.Append(InlineNodeToText(node));
        }
        var valueRaw = sb.ToString();
        // Strip leading separators like ":", "—", spaces
        valueRaw = Regex.Replace(valueRaw, @"^[\t\s:：—-]+", "");
        return (label, valueRaw);
    }

    private static string NormalizeLabel(string s)
    {
        s = s.Trim().TrimEnd(':', '：');
        return s.ToLowerInvariant();
    }

    private static List<string> SplitList(string s)
    {
        var replaced = s.Replace(" и ", ",", StringComparison.OrdinalIgnoreCase);
        return replaced.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static string InlineToText(Markdig.Syntax.Inlines.ContainerInline? inline)
    {
        if (inline is null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var child in inline)
        {
            sb.Append(InlineNodeToText(child));
        }
        return sb.ToString().Trim();
    }

    private static string InlineNodeToText(Markdig.Syntax.Inlines.Inline node)
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
