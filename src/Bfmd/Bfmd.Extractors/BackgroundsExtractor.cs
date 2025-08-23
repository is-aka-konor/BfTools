using System.Text;
using System.Text.RegularExpressions;
using Bfmd.Core.Config;
using BfCommon.Domain.Models;
using Bfmd.Core.Pipeline;
using Markdig.Syntax;

namespace Bfmd.Extractors;

public class BackgroundsExtractor : IExtractor
{
    public IEnumerable<BaseEntity> Extract(IEnumerable<(string path, string content, MarkdownDocument doc, string sha256)> docs, SourceItem src, MappingConfig map)
    {
        foreach (var (path, content, doc, _) in docs)
        {
            foreach (var bg in ParseBackgrounds(doc, content, path, map))
                yield return bg;
        }
    }

    internal static IEnumerable<BackgroundDto> ParseBackgrounds(MarkdownDocument doc, string content, string sourcePath, MappingConfig map)
    {
        var headerLevel = map.EntryHeaderLevel;
        // Find the parent section (e.g., H2 "ПРЕДЫСТОРИИ") and restrict entries to that span
        var (sectionStart, sectionEnd) = FindSection(doc, 2, map.EntrySectionHeaders);
        var h3s = doc.Descendants().OfType<HeadingBlock>()
            .Where(h => h.Level == headerLevel)
            .Where(h => sectionStart == null || (h.Span.Start > sectionStart.Span.Start && (sectionEnd == null || h.Span.Start < sectionEnd.Span.Start)))
            .ToList();
        for (int i = 0; i < h3s.Count; i++)
        {
            var h = h3s[i];
            var name = InlineToText(h.Inline);
            var until = NextBoundary(doc, h, i + 1 < h3s.Count ? h3s[i + 1] : null);

            var talentHeader = until.OfType<HeadingBlock>().FirstOrDefault(hb => hb.Level == headerLevel + 1 && HeaderMatches(hb, map.TalentHeaders));

            var bg = new BackgroundDto
            {
                Type = "background",
                Name = name,
                SkillProficiencies = new SkillsPickDto(),
                ToolProficiencies = new SkillsPickDto(),
                Languages = new SkillsPickDto(),
                Equipment = new List<string>(),
                Additional = new List<string>(),
                TalentOptions = new TalentOptionsDto { Choose = 1, From = new List<string>() },
                SourceFile = sourcePath
            };

            // Description markdown (everything before talent header within section)
            var descBlocks = until.TakeWhile(b => talentHeader == null || !ReferenceEquals(b, talentHeader)).ToList();
            bg.Description = BlocksToMarkdown(content, descBlocks);

            // Talent section parsing
            if (talentHeader != null)
            {
                var talentBlocks = until.SkipWhile(b => !ReferenceEquals(b, talentHeader)).Skip(1) // skip the H4 itself
                    .ToList();
                bg.TalentDescription = BlocksToMarkdown(content, talentBlocks);

                // Parse first paragraph after talent header for options
                var nextPara = NextParagraph(talentHeader);
                if (nextPara is ParagraphBlock p)
                {
                    var ptext = InlineToText(p.Inline);
                    bg.TalentOptions = ParseTalentOptions(ptext);
                }
            }

            foreach (var block in until)
            {
                if (block is ListBlock list)
                {
                    foreach (var item in list)
                    {
                        if (item is ListItemBlock li)
                        {
                            var text = FlattenText(li);
                            if (HeaderTextMatches(text, map.SkillsHeaders))
                            {
                                bg.SkillProficiencies = ParseSkillsPick(text);
                            }
                            else if (HeaderTextMatches(text, map.EquipmentHeaders))
                            {
                                bg.Equipment = ParseEquipment(text);
                            }
                            else if (text.Contains("Дополнительные", StringComparison.OrdinalIgnoreCase))
                            {
                                bg.Additional = ParseAdditional(text);
                            }
                        }
                    }
                }
            }

            bg.SkillProficiencies ??= new SkillsPickDto { Choose = 0, From = new(), Granted = new() };
            yield return bg;
        }
    }

    internal static (HeadingBlock? start, HeadingBlock? end) FindSection(MarkdownDocument doc, int level, IEnumerable<string> headers)
    {
        var all = doc.Descendants().OfType<HeadingBlock>().ToList();
        HeadingBlock? start = null;
        foreach (var h in all)
        {
            if (h.Level == level)
            {
                var txt = InlineToText(h.Inline);
                if (headers.Any(x => txt.Contains(x, StringComparison.OrdinalIgnoreCase)))
                {
                    start = h; break;
                }
            }
        }
        if (start == null) return (null, null);
        var end = all.FirstOrDefault(h => h.Span.Start > start.Span.Start && h.Level <= level);
        return (start, end);
    }

    internal static IEnumerable<Block> NextBoundary(MarkdownDocument doc, Block start, Block? nextH3)
    {
        var result = new List<Block>();
        bool within = false;
        foreach (var b in doc)
        {
            if (!within)
            {
                if (ReferenceEquals(b, start)) within = true;
                continue;
            }
            if (nextH3 != null && ReferenceEquals(b, nextH3)) break;
            if (b is ThematicBreakBlock) break; // '---'
            result.Add(b);
        }
        return result;
    }

    internal static Block? NextParagraph(Block from)
    {
        if (from.Parent is not ContainerBlock container) return null;
        bool pick = false;
        foreach (var child in container)
        {
            if (!pick)
            {
                if (ReferenceEquals(child, from)) pick = true;
                continue;
            }
            if (child is ParagraphBlock p) return p;
        }
        return null;
    }

    internal static SkillsPickDto ParseSkillsPick(string text)
    {
        int choose = 0;
        var mNum = Regex.Match(text, "Выберите\\s+(\\d+|один|два|три)", RegexOptions.IgnoreCase);
        if (mNum.Success)
        {
            var v = mNum.Groups[1].Value.ToLowerInvariant();
            choose = v switch { "один" => 1, "два" => 2, "три" => 3, _ => int.TryParse(v, out var n) ? n : 0 };
        }
        // Options usually follow the last colon
        var idx = text.LastIndexOf(':');
        var list = idx >= 0 ? text[(idx + 1)..] : text;
        var opts = SplitItems(list);
        return new SkillsPickDto { Choose = choose, From = opts, Granted = new List<string>() };
    }

    internal static List<string> ParseEquipment(string text)
    {
        var idx = text.IndexOf(':');
        var s = idx >= 0 ? text[(idx + 1)..] : text;
        return SplitItems(s);
    }

    internal static TalentOptionsDto ParseTalentOptions(string paragraph)
    {
        var idx = paragraph.LastIndexOf(':');
        var list = idx >= 0 ? paragraph[(idx + 1)..] : paragraph;
        var items = SplitItems(list);
        return new TalentOptionsDto { Choose = 1, From = items };
    }

    internal static List<string> ParseAdditional(string text)
    {
        var idx = text.IndexOf(':');
        var s = idx >= 0 ? text[(idx + 1)..] : text;
        return SplitItems(s);
    }

    internal static List<string> SplitItems(string s)
    {
        var replaced = s.Replace(" или ", ",", StringComparison.OrdinalIgnoreCase)
                        .Replace(" и ", ",", StringComparison.OrdinalIgnoreCase);
        return replaced.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Select(Normalize)
                       .Where(x => !string.IsNullOrWhiteSpace(x))
                       .ToList();
    }

    internal static string Normalize(string t)
    {
        var s = t.Trim();
        s = Regex.Replace(s, "^[—-]\\s*", "");
        s = Regex.Replace(s, "[\\.\\!\\?]$", "");
        return s;
    }

    internal static bool HeaderMatches(HeadingBlock hb, IEnumerable<string> headers)
    {
        var text = InlineToText(hb.Inline);
        return headers.Any(h => text.Contains(h, StringComparison.OrdinalIgnoreCase));
    }

    internal static bool HeaderTextMatches(string text, IEnumerable<string> headers)
    {
        return headers.Any(h => text.Contains(h, StringComparison.OrdinalIgnoreCase));
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

    internal static string InlineToText(Markdig.Syntax.Inlines.ContainerInline? inline)
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

    internal static string FlattenText(ListItemBlock li)
    {
        var sb = new StringBuilder();
        foreach (var b in li)
        {
            if (b is ParagraphBlock p) sb.Append(InlineToText(p.Inline));
        }
        return sb.ToString();
    }
}
