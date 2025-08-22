using BfSiteGen.Core.Services;
using Xunit;

namespace BfSiteGen.Tests.Unit;

public class MarkdownRendererTests
{
    private readonly ITestOutputHelper _output;

    public MarkdownRendererTests(ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact]
    public void ToHtml_Strips_Disallowed_Html_And_Renders_Markdown()
    {
        var md = new MarkdownRenderer();
        var input = "# Title\n\n<script>alert('x')</script>Text with <b>bold</b> and *emphasis*.\n\n| H | I |\n|---|---|\n| 1 | 2 |";

        var html = md.ToHtml(input);
        _output.WriteLine(html);

        Assert.Contains("<h1", html);
        Assert.Contains(">Title<", html);
        // Disallowed raw HTML is removed
        Assert.DoesNotContain("<script>", html);
        // Markdown emphasis is rendered
        Assert.Contains("<em>emphasis</em>", html);
        // Tables extension renders table tags
        Assert.Contains("<table", html);
    }
}
