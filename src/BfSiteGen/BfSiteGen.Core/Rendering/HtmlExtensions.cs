using System.Text;
using BfCommon.Domain.Models;
using BfSiteGen.Core.Services;

namespace BfSiteGen.Core.Rendering;

public static class HtmlExtensions
{
    public static string ToHtml(this TalentDto t, IMarkdownRenderer renderer)
    {
        var md = t.Description ?? string.Empty;
        if (t.Benefits is { Count: > 0 })
        {
            var sb = new StringBuilder();
            sb.Append(md);
            if (!string.IsNullOrWhiteSpace(md)) sb.Append("\n\n");
            foreach (var b in t.Benefits)
            {
                sb.Append("* ").Append(b).Append('\n');
            }
            md = sb.ToString();
        }
        return renderer.RenderBlock(md);
    }

    public static string ToHtml(this BackgroundDto b, IMarkdownRenderer renderer)
        => renderer.RenderBlock(b.Description ?? string.Empty);

    public static string ToHtml(this ClassDto c, IMarkdownRenderer renderer)
        => renderer.RenderBlock(c.Description ?? string.Empty);

    public static string ToHtml(this LineageDto l, IMarkdownRenderer renderer)
        => renderer.RenderBlock(l.Description ?? string.Empty);

    public static string ToHtml(this SpellDto s, IMarkdownRenderer renderer)
    {
        // Build markdown on the fly from structured fields when Effect is present; fallback to Description
        if (s.Effect is { Count: > 0 })
        {
            var md = string.Join("\n\n", s.Effect);
            return renderer.RenderBlock(md);
        }
        return renderer.RenderBlock(s.Description ?? string.Empty);
    }
}
