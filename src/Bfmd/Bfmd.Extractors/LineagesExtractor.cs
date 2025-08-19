using Bfmd.Core.Config;
using Bfmd.Core.Domain;
using Bfmd.Core.Pipeline;
using Markdig.Syntax;

namespace Bfmd.Extractors;

public class LineagesExtractor : IExtractor
{
    public IEnumerable<BaseEntity> Extract(IEnumerable<(string path, MarkdownDocument doc, string sha256)> docs, SourceItem src, MappingConfig map)
    {
        foreach (var (path, doc, _) in docs)
        {
            var title = doc.Descendants().OfType<HeadingBlock>().FirstOrDefault(h => h.Level == 1)?.Inline?.FirstChild?.ToString();
            if (string.IsNullOrWhiteSpace(title)) continue;
            yield return new LineageDto
            {
                Type = "lineage",
                Name = title!,
                Size = "Medium",
                Speed = 30,
                Traits = [new() { Name = "Trait", Description = "Sample" }],
                SourceFile = path
            };
        }
    }
}

