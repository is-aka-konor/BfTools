using System.Text;
using System.Text.RegularExpressions;
using Bfmd.Core.Config;
using BfCommon.Domain.Models;
using Bfmd.Core.Pipeline;
using Markdig.Syntax;
using Serilog;

namespace Bfmd.Extractors;

public class SpellsExtractor : IExtractor
{
    public IEnumerable<BaseEntity> Extract(IEnumerable<(string path, string content, MarkdownDocument doc, string sha256)> docs, SourceItem src, MappingConfig map)
    {
        foreach (var (path, content, doc, _) in docs)
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
                var headerText = InlineToText(h2.Inline);
                if (!TryParseHeaderParts(headerText, out var nameRu, out var slug))
                {
                    LogWarn(headerText, "invalid header format; expected 'Name | Slug'");
                    continue;
                }

                var (circle, hasCircle, traditions, school, casting, range, components, duration, effect, isRitual) = ParseSpellBody(blocks);
                if (!hasCircle)
                {
                    LogWarn(nameRu, "missing or invalid circle");
                    continue;
                }
                if (effect.Count == 0)
                {
                    LogWarn(nameRu, "missing effect text");
                    continue;
                }
                if (!isRitual.HasValue)
                {
                    LogWarn(nameRu, "missing ritual flag");
                }

                yield return new SpellDto
                {
                    Type = "spell",
                    Name = nameRu,
                    Slug = slug,
                    Circle = circle,
                    IsRitual = isRitual ?? false,
                    Circles = traditions,
                    School = school,
                    CastingTime = casting,
                    Range = range,
                    Components = components,
                    Duration = duration,
                    Effect = effect,
                    Description = BlocksToMarkdown(content, blocks),
                    SourceFile = path
                };
            }
        }
    }

    internal static List<Block> FindNextBreakOrH2(MarkdownDocument doc, Block start, HeadingBlock? nextH2)
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

    internal static bool TryParseHeaderParts(string headingText, out string name, out string slug)
    {
        name = string.Empty;
        slug = string.Empty;
        var text = headingText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var split = text.Split('|', 2, StringSplitOptions.TrimEntries);
        if (split.Length < 2) return false;
        name = SanitizeName(split[0].Trim());
        slug = SanitizeSlug(split[1].Trim());
        return !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(slug);
    }

    internal static string SanitizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var cleaned = value.Replace("/", " ").Replace("\\", " ");
        return Regex.Replace(cleaned, "\\s+", " ").Trim();
    }

    internal static string SanitizeSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var cleaned = value.Replace("/", "-").Replace("\\", "-");
        cleaned = Regex.Replace(cleaned, "-{2,}", "-");
        return cleaned.Trim(' ', '-');
    }

    internal static (int circle, bool hasCircle, List<string> traditions, string school, string casting, string range, string components, string duration, List<string> effect, bool? isRitual)
        ParseSpellBody(IReadOnlyList<Block> blocks)
    {
        int circle = 0; var traditions = new List<string>(); var school = string.Empty;
        string casting = string.Empty, range = string.Empty, components = string.Empty, duration = string.Empty;
        var effect = new List<string>();
        bool? isRitual = null;
        bool hasCircle = false;

        for (int bi = 0; bi < blocks.Count; bi++)
        {
            if (blocks[bi] is not ListBlock list) continue;

            var labeled = ParseListBlock(list);

            if (labeled.TryGetValue("уровень", out var lvlRaw))
            {
                var ru = lvlRaw.Split('/')[0].Trim();
                hasCircle = ParseLevelLine(ru, out circle, out traditions, out school);
            }
            if (labeled.TryGetValue("ритуал", out var ritualRaw))
            {
                var ritual = ritualRaw.Trim().TrimEnd('.');
                if (ritual.Equals("да", StringComparison.OrdinalIgnoreCase)) isRitual = true;
                else if (ritual.Equals("нет", StringComparison.OrdinalIgnoreCase)) isRitual = false;
            }
            if (labeled.TryGetValue("время накладывания", out var castingRaw)) casting = castingRaw.Trim();
            if (labeled.TryGetValue("дистанция", out var rangeRaw)) range = rangeRaw.Trim();
            if (labeled.TryGetValue("компоненты", out var compRaw)) components = compRaw.Trim();
            if (labeled.TryGetValue("длительность", out var durRaw)) duration = durRaw.Trim();

            if (labeled.TryGetValue("эффект", out var effRaw))
            {
                if (!string.IsNullOrWhiteSpace(effRaw)) effect.Add(effRaw.Trim());
                var extras = CollectEffect(blocks, bi + 1);
                effect.AddRange(extras);
            }
        }

        return (circle, hasCircle, traditions, school, casting, range, components, duration, effect, isRitual);
    }

    // removed FollowingBlocks to avoid multiple enumeration and improve performance

    internal static bool ParseLevelLine(string russian, out int circle, out List<string> traditions, out string school)
    {
        circle = 0; traditions = new List<string>(); school = string.Empty;
        var m = Regex.Match(russian, @"(\d+)\s*-?й\s+круг", RegexOptions.IgnoreCase);
        var hasCircle = m.Success && int.TryParse(m.Groups[1].Value, out circle);
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
        return hasCircle;
    }

    internal static (string label, string value) ExtractLabeledValue(ParagraphBlock p)
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

    internal static string NormalizeLabel(string s)
    {
        s = s.Trim().TrimEnd(':', '：');
        return s.ToLowerInvariant();
    }

    internal static List<string> SplitList(string s)
    {
        var replaced = s.Replace(" и ", ",", StringComparison.OrdinalIgnoreCase);
        return replaced.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    internal static string InlineToText(Markdig.Syntax.Inlines.ContainerInline? inline)
    {
        if (inline is null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var child in inline)
        {
            sb.Append(InlineNodeToText(child));
        }
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

    // Parse a list of labeled bullets into a normalized dictionary
    internal static Dictionary<string, string> ParseListBlock(ListBlock list)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in list)
        {
            if (item is not ListItemBlock li) continue;
            var p = li.Descendants().OfType<ParagraphBlock>().FirstOrDefault();
            if (p is null) continue;
            var (label, value) = ExtractLabeledValue(p);
            if (string.IsNullOrWhiteSpace(label)) continue;
            dict[NormalizeLabel(label)] = value;
        }
        return dict;
    }

    // Collect effect continuation blocks after the list (headings and paragraphs)
    internal static List<string> CollectEffect(IReadOnlyList<Block> blocks, int fromIndex)
    {
        var effect = new List<string>();
        for (int i = fromIndex; i < blocks.Count; i++)
        {
            var f = blocks[i];
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
        return effect;
    }

    internal static string BlocksToMarkdown(string content, List<Block> blocks)
    {
        if (blocks.Count == 0) return string.Empty;
        var start = blocks.First().Span.Start;
        var end = blocks.Last().Span.End;
        if (start < 0 || end < 0 || end < start || end + 1 > content.Length) return string.Empty;
        var slice = content.Substring(start, end - start + 1);
        return slice.Trim();
    }

    internal static void LogWarn(string name, string message)
    {
        Log.Warning("Spell parse warning: {SpellName} {Issue}", name, message);
    }
}
