using Markdig;
using Markdig.Syntax;

namespace Bfmd.Core.Services;

public static class MarkdownAst
{
    private static readonly Lazy<MarkdownPipeline> _pipeline = new(() =>
        new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .UseEmojiAndSmiley()
            .UsePipeTables()
            .UsePreciseSourceLocation()
            .UseListExtras()
            .UseAutoIdentifiers()
            .Build());

    public static MarkdownPipeline Pipeline => _pipeline.Value;

    public static MarkdownDocument Parse(string content)
        => Markdown.Parse(content, Pipeline);
}
