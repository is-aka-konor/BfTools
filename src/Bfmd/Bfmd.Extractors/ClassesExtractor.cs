using System.Text;
using System.Text.RegularExpressions;
using Bfmd.Core.Config;
using BfCommon.Domain.Models;
using Bfmd.Core.Pipeline;
using Markdig.Syntax;

namespace Bfmd.Extractors;

public class ClassesExtractor : IExtractor
{
    public IEnumerable<BaseEntity> Extract(IEnumerable<(string path, string content, MarkdownDocument doc, string sha256)> docs, SourceItem src, MappingConfig map)
    {
        foreach (var (path, content, doc, _) in docs)
        {
            var (title, classHeader) = GetHeadingTextAndNode(doc, map.EntryHeaderLevel);
            if (string.IsNullOrWhiteSpace(title)) continue;
            var sectionBlocks = GetSectionBlocks(doc, classHeader);
            var desc = SectionMarkdown(doc, content, classHeader, sectionBlocks);

            var cls = new ClassDto
            {
                Type = "class",
                Name = title!,
                Description = desc,
                Summary = GetFirstParagraph(doc),
                HitDie = GuessHitDie(sectionBlocks),
                SavingThrows = ParseSavingThrows(sectionBlocks, map),
                Proficiencies = new ProficienciesDto
                {
                    Skills = ParseSkills(sectionBlocks, map),
                    Armor = ParseArmor(sectionBlocks, map),
                    Weapons = ParseWeapons(sectionBlocks, map),
                    Tools = ParseTools(sectionBlocks, map)
                },
                StartingEquipment = new StartingEquipmentDto { Items = ParseStartingEquipment(sectionBlocks, map) },
                Levels = Enumerable.Range(1, 20).Select(i => new LevelRowDto { Level = i, ProficiencyBonus = $"+{((i - 1) / 4) + 2}", Features =
                    []
                }).ToList(),
                Features = [],
                Subclasses = [],
                SourceFile = path
            };

            // Parse features from the progression table under class features
            try
            {
                var featuresByLevel = ParseProgressionFeatures(sectionBlocks);
                foreach (var (lvl, feats) in featuresByLevel)
                {
                    var row = cls.Levels.FirstOrDefault(r => r.Level == lvl);
                    if (row != null) row.Features = feats;
                }
            }
            catch
            {
                // ignore parsing errors to keep extraction resilient
            }

            yield return cls;
        }
    }

    internal static string GetFirstParagraph(MarkdownDocument doc)
    {
        var p = doc.Descendants().OfType<ParagraphBlock>().FirstOrDefault();
        return p is null ? string.Empty : InlineToText(p.Inline);
    }

    internal static string GuessHitDie(IEnumerable<Block> blocks)
    {
        var text = new StringBuilder();
        foreach (var b in blocks)
        {
            if (b is ParagraphBlock p) text.AppendLine(InlineToText(p.Inline));
            if (b is ListBlock l)
            {
                foreach (var i in l) if (i is ListItemBlock li)
                {
                    foreach (var c in li) if (c is ParagraphBlock pi) text.AppendLine(InlineToText(pi.Inline));
                }
            }
        }
        var s = text.ToString();
        var m = Regex.Match(s, "1d(12|10|8|6)", RegexOptions.IgnoreCase);
        return m.Success ? "d" + m.Groups[1].Value : "d8";
    }

    internal static (string title, HeadingBlock? node) GetHeadingTextAndNode(MarkdownDocument doc, int preferredLevel)
    {
        HeadingBlock? h = doc.Descendants().OfType<HeadingBlock>().FirstOrDefault(x => x.Level == preferredLevel);
        if (h == null) h = doc.Descendants().OfType<HeadingBlock>().FirstOrDefault(x => x.Level is 1 or 2);
        return h is null ? (string.Empty, null) : (InlineToText(h.Inline), h);
    }

    internal static List<Block> GetSectionBlocks(MarkdownDocument doc, HeadingBlock? start)
    {
        var blocks = new List<Block>();
        if (start == null) return blocks;
        bool within = false;
        foreach (var b in doc)
        {
            if (!within)
            {
                if (ReferenceEquals(b, start)) within = true;
                continue;
            }
            if (b is HeadingBlock hb && hb.Level <= start.Level) break;
            blocks.Add(b);
        }
        return blocks;
    }

    internal static List<string> ParseStartingEquipment(IEnumerable<Block> blocks, MappingConfig map)
    {
        var header = FindHeader(blocks, map, m => m.StartingEquipmentHeaders);
        if (header == null) return new List<string>();
        var items = new List<string>();
        foreach (var b in EnumerateFollowingBlocks(blocks, header))
        {
            if (b is HeadingBlock) break;
            if (b is ListBlock list)
            {
                foreach (var it in list)
                {
                    if (it is ListItemBlock li)
                    {
                        var sb = new StringBuilder();
                        foreach (var c in li)
                        {
                            if (c is ParagraphBlock p) sb.Append(InlineToText(p.Inline));
                        }
                        var t = sb.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(t)) items.Add(t);
                    }
                }
            }
        }
        return items;
    }

    internal static List<string> ParseSavingThrows(IEnumerable<Block> blocks, MappingConfig map)
    {
        var header = FindHeader(blocks, map, _ => map.ProficienciesHeaders);
        if (header == null) return new List<string>();
        foreach (var b in EnumerateFollowingBlocks(blocks, header))
        {
            if (b is HeadingBlock) break;
            if (b is ListBlock list)
            {
                foreach (var it in list)
                {
                    if (it is ListItemBlock li)
                    {
                        var text = FlattenText(li);
                        if (text.StartsWith("Спасброски", StringComparison.OrdinalIgnoreCase))
                        {
                            var idx = text.IndexOf(':');
                            var s = idx >= 0 ? text[(idx + 1)..] : text;
                            return SplitItems(s);
                        }
                    }
                }
            }
        }
        return new List<string>();
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

    internal static string FlattenText(ListItemBlock li)
    {
        var sb = new StringBuilder();
        foreach (var b in li)
        {
            if (b is ParagraphBlock p) sb.Append(InlineToText(p.Inline));
        }
        return sb.ToString();
    }

    internal static SkillsPickDto ParseSkills(IEnumerable<Block> blocks, MappingConfig map)
    {
        var res = new SkillsPickDto { Choose = 0, From = new List<string>(), Granted = new List<string>() };
        var header = FindHeader(blocks, map, _ => map.ProficienciesHeaders);
        if (header == null) return res;
        foreach (var b in EnumerateFollowingBlocks(blocks, header))
        {
            if (b is HeadingBlock) break;
            if (b is ListBlock list)
            {
                foreach (var it in list)
                {
                    if (it is ListItemBlock li)
                    {
                        var text = FlattenText(li);
                        if (text.StartsWith("Навыки", StringComparison.OrdinalIgnoreCase))
                        {
                            res.Choose = ExtractChoose(text);
                            var hasFrom = text.Contains("из следующих", StringComparison.OrdinalIgnoreCase);
                            var idx = text.LastIndexOf(':');
                            var s = idx >= 0 ? text[(idx + 1)..] : text;
                            if (hasFrom)
                            {
                                res.From = SplitItems(s);
                            }
                            else
                            {
                                res.From = new List<string> { "ANY" };
                            }
                            return res;
                        }
                    }
                }
            }
        }
        return res;
    }

    internal static List<string> ParseArmor(IEnumerable<Block> blocks, MappingConfig map)
        => ParseProfLine(blocks, map, "Доспехи");

    internal static List<string> ParseWeapons(IEnumerable<Block> blocks, MappingConfig map)
        => ParseProfLine(blocks, map, "Оружие");

    internal static List<string> ParseTools(IEnumerable<Block> blocks, MappingConfig map)
        => ParseProfLine(blocks, map, "Инструменты");

    internal static List<string> ParseProfLine(IEnumerable<Block> blocks, MappingConfig map, string label)
    {
        var header = FindHeader(blocks, map, _ => map.ProficienciesHeaders);
        if (header == null) return new List<string>();
        foreach (var b in EnumerateFollowingBlocks(blocks, header))
        {
            if (b is HeadingBlock) break;
            if (b is ListBlock list)
            {
                foreach (var it in list)
                {
                    if (it is ListItemBlock li)
                    {
                        var text = FlattenText(li);
                        if (text.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                        {
                            var idx = text.IndexOf(':');
                            var s = idx >= 0 ? text[(idx + 1)..] : text;
                            return SplitItems(s);
                        }
                    }
                }
            }
        }
        return new List<string>();
    }

    internal static int ExtractChoose(string text)
    {
        var m = Regex.Match(text, "Выберите\\s+(\\d+|два|три|четыре)", RegexOptions.IgnoreCase);
        if (!m.Success) m = Regex.Match(text, "Любые\\s+(\\d+|два|три|четыре)", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var v = m.Groups[1].Value.ToLowerInvariant();
            return v switch
            {
                "два" => 2,
                "три" => 3,
                "четыре" => 4,
                _ => int.TryParse(v, out var n) ? n : 0
            };
        }
        return 0;
    }

    internal static HeadingBlock? FindHeader(IEnumerable<Block> blocks, MappingConfig map, Func<MappingConfig, IEnumerable<string>> selector)
    {
        var headers = selector(map) ?? Array.Empty<string>();
        foreach (var b in blocks)
        {
            if (b is HeadingBlock hb)
            {
                var t = InlineToText(hb.Inline);
                if (headers.Any(h => t.Contains(h, StringComparison.OrdinalIgnoreCase))) return hb;
            }
        }
        return null;
    }

    internal static IEnumerable<Block> EnumerateFollowingBlocks(IEnumerable<Block> all, Block header)
    {
        bool after = false;
        foreach (var b in all)
        {
            if (!after)
            {
                if (ReferenceEquals(b, header)) after = true;
                continue;
            }
            yield return b;
        }
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
    internal static string BlocksToMarkdown(string content, List<Block> blocks)
    {
        if (blocks.Count == 0) return string.Empty;
        var start = blocks.First().Span.Start;
        var end = blocks.Last().Span.End;
        if (start < 0 || end < 0 || end < start || end + 1 > content.Length) return string.Empty;
        var slice = content.Substring(start, end - start + 1);
        return slice.Trim();
    }

    internal static string SectionMarkdown(MarkdownDocument doc, string content, HeadingBlock? header, List<Block> blocks)
    {
        if (header == null) return string.Empty;
        // Prefer blocks-based slice (excludes the header line)
        var byBlocks = BlocksToMarkdown(content, blocks);
        if (!string.IsNullOrWhiteSpace(byBlocks)) return byBlocks;
        // Fallback: from after header to before next peer/superior header
        var allHeads = doc.Descendants().OfType<HeadingBlock>().ToList();
        var next = allHeads.FirstOrDefault(h => h.Span.Start > header.Span.Start && h.Level <= header.Level);
        var start = header.Span.End + 1;
        var end = next != null ? next.Span.Start - 1 : content.Length - 1;
        if (start < 0 || end < start || end >= content.Length) return string.Empty;
        return content.Substring(start, end - start + 1).Trim();
    }

    // New: Parse features per level from the "ПРОГРЕССИЯ" table
    internal static Dictionary<int, List<string>> ParseProgressionFeatures(IEnumerable<Block> blocks)
    {
        var result = new Dictionary<int, List<string>>();
        var (progressHeader, table) = FindProgressionTable(blocks);
        if (progressHeader == null || table == null) return result;
        var idx = FindColumnIndex(table, header => NormalizeHeader(header).Contains("умения"));
        if (idx < 0) return result;

        foreach (var rowObj in table)
        {
            if (rowObj is not Markdig.Extensions.Tables.TableRow tr) continue;
            // try skip header rows by reflection flag or by position
            var rowIsHeaderProp = tr.GetType().GetProperty("IsHeader");
            if (rowIsHeaderProp != null && rowIsHeaderProp.PropertyType == typeof(bool) && (bool)(rowIsHeaderProp.GetValue(tr) ?? false))
                continue;

            var cells = new List<Markdig.Extensions.Tables.TableCell>();
            foreach (var cellObj in tr)
            {
                if (cellObj is Markdig.Extensions.Tables.TableCell cell) cells.Add(cell);
            }
            if (cells.Count == 0) continue;
            var level = ParseLevelNumber(TableCellToText(cells[0]));
            if (level < 1 || level > 20) continue;
            if (idx >= cells.Count) continue;
            var featsText = TableCellToText(cells[idx]);
            var feats = SplitItems(featsText);
            result[level] = feats;
        }
        return result;
    }

    internal static (HeadingBlock? header, Markdig.Extensions.Tables.Table? table) FindProgressionTable(IEnumerable<Block> blocks)
    {
        HeadingBlock? progress = null;
        foreach (var b in blocks)
        {
            if (b is HeadingBlock hb)
            {
                var t = InlineToText(hb.Inline);
                if (t.Contains("ПРОГРЕССИЯ", StringComparison.OrdinalIgnoreCase))
                {
                    progress = hb; break;
                }
            }
        }
        if (progress == null) return (null, null);
        foreach (var b in EnumerateFollowingBlocks(blocks, progress))
        {
            if (b is HeadingBlock) break;
            if (b is Markdig.Extensions.Tables.Table tbl) return (progress, tbl);
        }
        return (progress, null);
    }

    internal static int FindColumnIndex(Markdig.Extensions.Tables.Table table, Func<string, bool> predicate)
    {
        // find first TableRow (header)
        Markdig.Extensions.Tables.TableRow? headerRow = null;
        foreach (var obj in table)
        {
            if (obj is Markdig.Extensions.Tables.TableRow r)
            {
                headerRow = r; break;
            }
        }
        if (headerRow == null) return -1;
        var headerCells = new List<Markdig.Extensions.Tables.TableCell>();
        foreach (var c in headerRow)
        {
            if (c is Markdig.Extensions.Tables.TableCell tc) headerCells.Add(tc);
        }
        for (int i = 0; i < headerCells.Count; i++)
        {
            var header = TableCellToText(headerCells[i]);
            if (predicate(header)) return i;
        }
        return -1;
    }

    internal static string TableCellToText(Markdig.Extensions.Tables.TableCell cell)
    {
        var sb = new StringBuilder();
        foreach (var b in cell)
        {
            if (b is ParagraphBlock p) sb.Append(InlineToText(p.Inline));
            else if (b is HeadingBlock h) sb.Append(InlineToText(h.Inline));
        }
        return sb.ToString().Trim();
    }

    internal static string NormalizeHeader(string s) => Regex.Replace(s, "\\s+", " ").Trim().ToLowerInvariant();

    internal static int ParseLevelNumber(string value)
    {
        // Expect formats like "1-й", "10-й"
        var m = Regex.Match(value, "(\\d+)");
        return m.Success && int.TryParse(m.Groups[1].Value, out var n) ? n : 0;
    }
}
