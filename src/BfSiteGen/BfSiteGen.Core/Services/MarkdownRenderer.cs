using Markdig;

namespace BfSiteGen.Core.Services;

public interface IMarkdownRenderer
{
    string ToHtml(string markdown);
}

public sealed class MarkdownRenderer : IMarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownRenderer()
    {
        var builder = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .DisableHtml(); // Strip embedded HTML for safety
        _pipeline = builder.Build();
    }

    public string ToHtml(string markdown) => string.IsNullOrWhiteSpace(markdown)
        ? string.Empty
        : Markdown.ToHtml(markdown, _pipeline);
}

