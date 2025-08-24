using BfCommon.Domain.Models;
using BfSiteGen.Core.Rendering;
using BfSiteGen.Core.Services;
using Xunit;

namespace BfSiteGen.Tests.Unit;

public class RendererEntitiesTests
{
    private static int CountOf(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle)) return 0;
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++; idx += needle.Length;
        }
        return count;
    }

    [Fact]
    public void TalentHtml_RendersBenefitsAsList()
    {
        var t = new TalentDto
        {
            Name = "Talent",
            Description = "Intro paragraph.",
            Benefits = new List<string> { "First benefit", "Second benefit" }
        };
        var r = new MarkdownRenderer();
        var html = t.ToHtml(r);
        Assert.Contains("<p", html);
        Assert.Contains("<ul", html);
        Assert.Contains("<li", html);
        Assert.Contains("First benefit", html);
        Assert.Contains("Second benefit", html);
        Assert.DoesNotContain("<script", html);
    }

    [Fact]
    public void ClassHtml_RendersHeadingsAndTable()
    {
        var md = string.Join('\n', new[]
        {
            "# Class Name",
            "",
            "| H | I |",
            "|---|---|",
            "| 1 | 2 |"
        });
        var c = new ClassDto { Name = "Class", Description = md };
        var r = new MarkdownRenderer();
        var html = c.ToHtml(r);
        Assert.Contains("<h1", html);
        Assert.Contains("<table", html);
        Assert.Contains("<td>1</td>", html);
        Assert.DoesNotContain("<script", html);
    }

    [Fact]
    public void SpellHtml_JoinsEffectIntoMultipleParagraphs()
    {
        var s = new SpellDto
        {
            Name = "Spell",
            Circle = 1,
            School = "Evocation",
            Effect = new List<string> { "First paragraph.", "Second paragraph." }
        };
        var r = new MarkdownRenderer();
        var html = s.ToHtml(r);
        Assert.True(CountOf(html, "<p") >= 2);
        Assert.Contains("First paragraph.", html);
        Assert.Contains("Second paragraph.", html);
        Assert.DoesNotContain("<script", html);
    }

    [Fact]
    public void BackgroundHtml_RendersParagraphsAndEmphasis()
    {
        var b = new BackgroundDto { Name = "BG", Description = "Some *emphasis* text." };
        var r = new MarkdownRenderer();
        var html = b.ToHtml(r);
        Assert.Contains("<p", html);
        Assert.Contains("<em>", html);
        Assert.DoesNotContain("<script", html);
    }

    [Fact]
    public void LineageHtml_RendersLinksSafely()
    {
        var l = new LineageDto { Name = "Lineage", Description = "A [link](https://example.com)." };
        var r = new MarkdownRenderer();
        var html = l.ToHtml(r);
        Assert.Contains("<p", html);
        Assert.Contains("<a href=\"https://example.com\"", html);
        Assert.DoesNotContain("<script", html);
    }
}

