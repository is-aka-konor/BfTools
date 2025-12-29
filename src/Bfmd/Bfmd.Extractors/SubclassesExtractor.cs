using BfCommon.Domain.Models;
using Bfmd.Core.Config;
using Bfmd.Core.Pipeline;
using Bfmd.Core.Services;
using Markdig.Syntax;

namespace Bfmd.Extractors;

public class SubclassesExtractor : IExtractor
{
    public IEnumerable<BaseEntity> Extract(IEnumerable<(string path, string content, MarkdownDocument doc, string sha256)> docs, SourceItem src, MappingConfig map)
    {
        foreach (var (path, content, doc, _) in docs)
        {
            var (parentName, parentHeader) = ClassesExtractor.GetHeadingTextAndNode(doc, map.EntryHeaderLevel);
            if (string.IsNullOrWhiteSpace(parentName) || parentHeader == null) continue;

            var firstHeading = doc.Descendants().OfType<HeadingBlock>().FirstOrDefault();
            if (firstHeading != null && !ReferenceEquals(firstHeading, parentHeader)) continue;

            var parentSlug = ClassesExtractor.SlugMap.GetValueOrDefault(parentName)
                ?? SlugService.From(parentName, cacheKey: parentName);

            foreach (var sub in ClassesExtractor.ExtractSubclassesFromDocument(doc, content, parentSlug, path, parentHeader))
                yield return sub;
        }
    }
}
