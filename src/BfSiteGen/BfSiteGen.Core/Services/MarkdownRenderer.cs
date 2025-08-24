using Markdig;

namespace BfSiteGen.Core.Services;

public interface IMarkdownRenderer
{
    // Preferred API for rendering markdown blocks
    string RenderBlock(string markdown);

    // Back-compat shim used by older call sites/tests
    string ToHtml(string markdown);
}

public sealed class MarkdownRenderer : IMarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownRenderer()
    {
        var builder = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // headings, lists, tables, task lists, etc.
            .DisableHtml(); // Strip embedded HTML for safety
        _pipeline = builder.Build();
    }

    public string RenderBlock(string markdown)
        => string.IsNullOrWhiteSpace(markdown) ? string.Empty : Markdown.ToHtml(markdown, _pipeline);

    public string ToHtml(string markdown) => RenderBlock(markdown);
}
