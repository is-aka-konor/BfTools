using Markdig;

namespace Bfmd.Core.Services;

public static class HtmlRenderer
{
    private static readonly Lazy<MarkdownPipeline> _htmlPipeline = new(() =>
        new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .UseEmojiAndSmiley()
            .UsePipeTables()
            .UseListExtras()
            .UseAutoIdentifiers()
            .Build());

    public static string ToHtml(string markdown)
        => Markdown.ToHtml(markdown, _htmlPipeline.Value);
}

