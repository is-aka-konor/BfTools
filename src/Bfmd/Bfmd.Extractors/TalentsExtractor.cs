using Bfmd.Core.Config;
using Bfmd.Core.Domain;
using Bfmd.Core.Pipeline;
using System.Text;
using System.Text.RegularExpressions;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Bfmd.Extractors;

// Assumptions:
// - BaseEntity, SourceItem, MappingConfig, TalentDto, SourceRef (or equivalent) already exist in your solution.
// - TalentDto : BaseEntity and has at least:
//     string Category, string? Requirement, List<string> Benefits, string Description
// - MappingConfig has at least: Dictionary<string,string> Regexes, Dictionary<string,string> Synonyms,
//   and optionally List<string> CollectionRootHeaders (or similar; handled gracefully if absent).
public class TalentsExtractor : IExtractor
{
    public IEnumerable<BaseEntity> Extract(
        IEnumerable<(string path, string content, MarkdownDocument doc, string sha256)> docs,
        SourceItem src,
        MappingConfig? map)
    {
        var results = new List<BaseEntity>();

        // Compile mapping-driven knobs (with safe fallbacks)
        int categoryLevel = GetInt(map?.Regexes, "categoryHeaderLevel", 3);
        int entryLevel    = GetInt(map?.Regexes, "entryHeaderLevel", 4);

        var rxRequirement   = CompileWithAstFallback(
            map?.Regexes,
            "requirementLineRegex",
            astFallback: @"^\s*(?:\*\s*)?(?:\*\*)?Требовани[её](?:\*\*)?[:：]\s*(.+)$");
        var rxBenefit       = CompileWithAstFallback(
            map?.Regexes,
            "benefitBulletRegex",
            astFallback: @"^(?!\s*(?:\*\s*)?(?:\*\*)?Требован[её](?:\*\*)?[:：]).+\S.*$");
        var rxListSplit     = Compile(map?.Regexes, "listSplitRegex",       @"(?:,|;|\s+и\s+|\s+или\s+)", RegexOptions.IgnoreCase);

        var rxBenefitIntro  = CompileMany(map?.Regexes, prefix: "benefitIntro");
        var rxRepeatable    = CompileMany(map?.Regexes, prefix: "repeatable");
        var rxChoiceIntro   = CompileMany(map?.Regexes, prefix: "choiceIntro");

        // Root scoping (## ТАЛАНТЫ) is optional; if configured use it to limit the parse range
        var rootHeaders = map?.CollectionRootHeaders ?? new List<string>();

        foreach (var (path, content, doc, sha256) in docs)
        {
            // Build a quick index of heading blocks to segment the document and to help raw-markdown slicing.
            var allBlocks = doc.ToList();

            // Establish the bounds we parse (optionally under "## ТАЛАНТЫ")
            var (startIdx, endIdx) = FindScopeBounds(allBlocks, rootHeaders, level: 2);

            string? currentCategory = null;

            for (int i = startIdx; i <= endIdx; i++)
            {
                if (allBlocks[i] is HeadingBlock hb)
                {
                    var text = HeadingText(hb);

                    // Category header?
                    if (hb.Level == categoryLevel && LooksLikeCategory(text))
                    {
                        currentCategory = NormalizeCategory(text, map);
                        continue;
                    }

                    // Talent entry header?
                    if (hb.Level == entryLevel)
                    {
                        // Collect all blocks until the next entry/category/root boundary
                        int j = i + 1;
                        while (j <= endIdx && !(allBlocks[j] is HeadingBlock h2 && (h2.Level == entryLevel || h2.Level == categoryLevel || h2.Level == 2)))
                            j++;

                        var entryBlocks = allBlocks.Skip(i).Take(j - i).ToList();
                        var name = text.Trim();

                        // Parse structured fields inside this entry block-range
                        var (requirement, benefits) = ParseRequirementAndBenefits(entryBlocks, rxRequirement, rxBenefit, rxBenefitIntro);

                        // Extract full raw markdown for rendering using precise spans
                        string descriptionMd = ExtractRawMarkdown(doc, content, hb);

                        // Build DTO (uses existing BaseEntity pattern)
                        var dto = new TalentDto
                        {
                            Type = "talent",
                            Name = name,
                            Category = currentCategory ?? "Общее",
                            Requirement = string.IsNullOrWhiteSpace(requirement) ? "" : requirement.Trim(),
                            Benefits = benefits,
                            Description = descriptionMd,
                            SourceFile = path,
                            Src = new SourceRef
                            {
                                Abbr = src.Abbr,
                                Name = src.Name,
                                Version = src.Version,
                                Url = src.Url,
                                License = src.License,
                                Hash = sha256
                            }
                        };

                        // Slug/Id (RU→lat; stable)
                        dto.Slug = Slugify(dto.Name);
                        dto.Id = $"{src.Abbr}:talent/{dto.Slug}";

                        results.Add(dto);
                        i = j - 1; // continue after this entry
                    }
                }
            }
        }

        return results;
    }

    // ------------- Parsing helpers -------------
    internal static (string? requirement, List<string> benefits) ParseRequirementAndBenefits(
        List<Block> entryBlocks,
        Regex rxRequirement,
        Regex rxBenefit,
        List<Regex> rxBenefitIntro)
    {
        string? requirement = null;
        var benefits = new List<string>();

        foreach (var lb in entryBlocks.OfType<ListBlock>())
        {
            foreach (var li in GetListItems(lb))
            {
                var para = li.Descendants<ParagraphBlock>().FirstOrDefault();
                string line;
                if (para != null)
                {
                    line = NormalizeSpaces(InlineToText(para.Inline)).Trim();
                }
                else
                {
                    var sb = new StringBuilder();
                    foreach (var lit in li.Descendants<LiteralInline>()) sb.Append(lit.Content.ToString());
                    line = NormalizeSpaces(sb.ToString()).Trim();
                }

                // Пропускаем вводные строки «Вы получаете следующие преимущества:»
                if (rxBenefitIntro.Any(rx => rx.IsMatch(line)))
                    continue;

                // Requirement via provided regex (with lenient fallback)
                var mReq = rxRequirement.Match(line);
                if (mReq.Success)
                {
                    var val = mReq.Groups.Count > 1 ? mReq.Groups[1].Value : string.Empty;
                    if (!string.IsNullOrWhiteSpace(val)) requirement ??= val.Trim();
                    continue;
                }
                // Fallback: plain starts-with detection and colon split
                if (line.StartsWith("требован", StringComparison.OrdinalIgnoreCase))
                {
                    var sep = line.IndexOf(':');
                    if (sep < 0) sep = line.IndexOf('：');
                    var val = sep >= 0 ? line[(sep + 1)..] : string.Empty;
                    if (!string.IsNullOrWhiteSpace(val)) requirement ??= val.Trim();
                    continue;
                }

                // Benefit via provided regex (with lenient fallback)
                var mBen = rxBenefit.Match(line);
                if (mBen.Success)
                {
                    var val = mBen.Groups.Count > 1 ? mBen.Groups[1].Value : line;
                    val = val.Trim();
                    if (val.Length > 0) benefits.Add(val);
                }
                else if (!line.StartsWith("требован", StringComparison.OrdinalIgnoreCase))
                {
                    benefits.Add(line);
                }
            }
        }

        return (requirement, benefits);
    }

    internal static (int startIdx, int endIdx) FindScopeBounds(List<Block> blocks, List<string> rootHeaders, int level)
    {
        if (rootHeaders == null || rootHeaders.Count == 0)
            return (0, blocks.Count - 1);

        // Find first H{level} whose text equals or contains any of rootHeaders (case-insensitive)
        int start = -1;
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] is HeadingBlock hb && hb.Level == level)
            {
                var t = HeadingText(hb);
                if (rootHeaders.Any(h => t.Contains(h.Trim(), StringComparison.OrdinalIgnoreCase) || h.Trim().Equals(t.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    start = i;
                    break;
                }
            }
        }
        if (start == -1)
        {
            // fallback: first header of the requested level
            start = blocks.FindIndex(b => b is HeadingBlock hb && hb.Level == level);
            if (start == -1) start = 0;
        }

        // End at next H{level} or EOF
        int end = blocks.Count - 1;
        for (int j = start + 1; j < blocks.Count; j++)
        {
            if (blocks[j] is HeadingBlock hb && hb.Level == level)
            {
                end = j - 1;
                break;
            }
        }

        return (start, end);
    }

    internal static IEnumerable<ListItemBlock> GetListItems(ListBlock lb)
    {
        // ListBlock наследует ContainerBlock и итерируется по дочерним Block;
        // выбираем только ListItemBlock без обращения к несуществующему .Children
        foreach (var child in lb)
        {
            if (child is ListItemBlock item) yield return item;
        }
    }
    
    internal static string NormalizeCategory(string raw, MappingConfig? map)
    {
        // Use synonyms table to normalize known category captions if provided.
        // We accept exact uppercase keys (e.g., "МАГИЧЕСКИЕ ТАЛАНТЫ") mapped to "Магические".
        var text = raw.Trim();

        // Try direct lookup in a "categorySynonyms" subspace of Regexes (mapping.talents.yaml recommended),
        // e.g., Regexes["categorySynonyms.МАГИЧЕСКИЕ ТАЛАНТЫ"] = "Магические"
        var normalized = TryLookupPrefixed(map?.Regexes, "categorySynonyms.", text.ToUpperInvariant());
        if (!string.IsNullOrEmpty(normalized)) return normalized!;

        // Fallback: strip the word "ТАЛАНТЫ"
        var up = text.ToUpperInvariant();
        if (up.Contains("ТАЛАНТ"))
            text = text.Replace("ТАЛАНТЫ", "", StringComparison.OrdinalIgnoreCase).Trim();

        // Capitalize first letter (RU-safe-ish)
        return text.Length == 0 ? "Общее" : char.ToUpper(text[0]) + text.Substring(1).ToLower();
    }

    internal static bool LooksLikeCategory(string text)
    {
        var up = text.ToUpperInvariant();
        return up.Contains("ТАЛАНТ"); // "МАГИЧЕСКИЕ ТАЛАНТЫ", "ВОИНСКИЕ ТАЛАНТЫ", etc.
    }

    // Precise slice using Markdig block spans for the entry heading to the next heading of same or higher level
    internal static string ExtractRawMarkdown(MarkdownDocument doc, string content, HeadingBlock entry)
    {
        int start = entry.Span.Start;
        // align to line start
        if (start > 0)
        {
            var idx = content.LastIndexOf('\n', Math.Min(start, content.Length - 1));
            start = idx >= 0 ? idx + 1 : 0;
        }
        // find next boundary: next heading with level <= this, or thematic break
        int end = content.Length;
        bool after = false;
        foreach (var b in doc)
        {
            if (!after)
            {
                if (ReferenceEquals(b, entry)) after = true;
                continue;
            }
            if (b is ThematicBreakBlock)
            {
                end = b.Span.Start; break;
            }
            if (b is HeadingBlock hb && hb.Level <= entry.Level)
            {
                end = hb.Span.Start; break;
            }
        }
        if (start < 0 || end < start || end > content.Length) return string.Empty;
        return content.Substring(start, end - start).TrimEnd();
    }

    internal static int IndexOfLineStart(string s, string marker, bool ignoreCase = false)
    {
        var comp = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        int idx = s.IndexOf(marker, comp);
        if (idx < 0) return -1;

        // ensure marker is at line start (previous char is start or '\n' or '\r')
        while (idx >= 0)
        {
            if (idx == 0 || s[idx - 1] == '\n' || s[idx - 1] == '\r')
                return idx;
            idx = s.IndexOf(marker, idx + 1, comp);
        }
        return -1;
    }

    internal static int FindNextBoundary(string s, int from)
    {
        int end = s.Length;

        int Pos(string token)
        {
            var p = s.IndexOf(token, from, StringComparison.Ordinal);
            return p >= 0 ? p : int.MaxValue;
        }

        // Note: look for boundaries as starting at line starts
        int pH4  = Pos("\n#### ");
        int pH3  = Pos("\n### ");
        int pH2  = Pos("\n## ");
        int pTB1 = Pos("\n---");
        int min = Math.Min(Math.Min(pH4, pH3), Math.Min(pH2, pTB1));

        if (min == int.MaxValue) return end;
        return min;
    }

    // ------------- Markdig helpers -------------

    internal static string HeadingText(HeadingBlock hb) => InlineToText(hb.Inline).Trim();

    internal static string InlineToText(Inline? node)
    {
        if (node is null) return string.Empty;
        var sb = new StringBuilder();
        if (node is ContainerInline cont)
        {
            for (var child = cont.FirstChild; child != null; child = child.NextSibling)
                AppendInline(child, sb);
        }
        else
        {
            AppendInline(node, sb);
        }
        return sb.ToString();
    }

    internal static void AppendInline(Inline node, StringBuilder sb)
    {
        switch (node)
        {
            case LiteralInline lit:
                sb.Append(lit.Content.ToString());
                break;
            case EmphasisInline emph:
                for (var child = emph.FirstChild; child != null; child = child.NextSibling)
                    AppendInline(child, sb);
                break;
            case LinkInline link:
                for (var child = link.FirstChild; child != null; child = child.NextSibling)
                    AppendInline(child, sb);
                break;
            case LineBreakInline:
                sb.Append(' ');
                break;
            case CodeInline code:
                sb.Append(code.Content);
                break;
            default:
                if (node is ContainerInline cont)
                {
                    for (var child = cont.FirstChild; child != null; child = child.NextSibling)
                        AppendInline(child, sb);
                }
                break;
        }
    }

    // ------------- Utilities -------------

    internal static Regex Compile(Dictionary<string,string>? dict, string key, string fallback, RegexOptions extra = RegexOptions.None)
    {
        var pattern = dict != null && dict.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;
        return new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | extra);
    }

    internal static Regex CompileWithAstFallback(Dictionary<string,string>? dict, string key, string astFallback, RegexOptions extra = RegexOptions.None)
    {
        string? provided = null;
        if (dict != null && dict.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v)) provided = v;
        var pattern = provided is null ? astFallback : $"(?:{provided})|(?:{astFallback})";
        return new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | extra | RegexOptions.Multiline);
    }

    internal static List<Regex> CompileMany(Dictionary<string,string>? dict, string prefix)
    {
        var list = new List<Regex>();
        if (dict != null)
        {
            foreach (var kv in dict)
            {
                if (kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(kv.Value))
                    list.Add(new Regex(kv.Value, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase));
            }
        }
        return list;
    }

    internal static int GetInt(Dictionary<string,string>? dict, string key, int @default)
    {
        if (dict != null && dict.TryGetValue(key, out var s) && int.TryParse(s, out var n)) return n;
        return @default;
    }

    internal static string? TryLookupPrefixed(Dictionary<string,string>? dict, string prefix, string key)
    {
        if (dict == null) return null;
        var full = prefix + key;
        if (dict.TryGetValue(full, out var v) && !string.IsNullOrWhiteSpace(v)) return v;
        return null;
    }

    internal static string NormalizeSpaces(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return Regex.Replace(s, @"\s+", " ").Trim();
    }

    internal static string Slugify(string name)
    {
        name = name.Trim().ToLowerInvariant();
        var map = new Dictionary<char, string>
        {
            ['а']="a",['б']="b",['в']="v",['г']="g",['д']="d",['е']="e",['ё']="yo",
            ['ж']="zh",['з']="z",['и']="i",['й']="y",['к']="k",['л']="l",['м']="m",
            ['н']="n",['о']="o",['п']="p",['р']="r",['с']="s",['т']="t",['у']="u",
            ['ф']="f",['х']="h",['ц']="ts",['ч']="ch",['ш']="sh",['щ']="sch",
            ['ъ']="",['ы']="y",['ь']="",['э']="e",['ю']="yu",['я']="ya"
        };
        var sb = new StringBuilder();
        foreach (var ch in name)
        {
            if (map.TryGetValue(ch, out var repl)) { sb.Append(repl); continue; }
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (char.IsWhiteSpace(ch) || ch=='_' || ch=='-' || ch=='—') sb.Append('-');
            // drop other punctuation
        }
        var slug = Regex.Replace(sb.ToString(), "-{2,}", "-").Trim('-');
        return slug;
    }
}

// Small extension used above
internal static class BlockExtensions
{
    public static IEnumerable<T> Descendants<T>(this ContainerBlock block) where T : class
    {
        foreach (var child in block)
        {
            if (child is T t) yield return t;
            if (child is ContainerBlock cb)
            {
                foreach (var d in cb.Descendants<T>()) yield return d;
            }
        }
    }

    private static bool StringEqualsIgnoreCaseAndTrim(string a, string b)
        => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}
