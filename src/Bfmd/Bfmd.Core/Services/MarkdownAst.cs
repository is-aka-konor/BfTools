using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;

namespace Bfmd.Core.Services;

public static class MarkdownAst
{
    private static readonly Lazy<MarkdownPipeline> _pipeline = new(() =>
        new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .UseEmojiAndSmiley()
            .UsePipeTables()
            .UseListExtras()
            .UseAutoIdentifiers()
            .Build());

    public static MarkdownPipeline Pipeline => _pipeline.Value;

    public static MarkdownDocument Parse(string content)
        => Markdig.Markdown.Parse(content, Pipeline);
}

