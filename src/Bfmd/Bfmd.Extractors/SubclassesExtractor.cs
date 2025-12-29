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
            var (parentName, parentSlug, parentHeader) = GetParentClassInfo(doc, map.EntryHeaderLevel);
            if (string.IsNullOrWhiteSpace(parentName) || parentHeader == null) continue;

            var firstHeading = doc.Descendants().OfType<HeadingBlock>().FirstOrDefault();
            if (firstHeading != null && !ReferenceEquals(firstHeading, parentHeader)) continue;

            foreach (var sub in ClassesExtractor.ExtractSubclassesFromDocument(doc, content, parentSlug, path, parentHeader))
                yield return sub;
        }
    }

    private static (string name, string slug, HeadingBlock? header) GetParentClassInfo(MarkdownDocument doc, int headerLevel)
    {
        var (title, header) = ClassesExtractor.GetHeadingTextAndNode(doc, headerLevel);
        if (string.IsNullOrWhiteSpace(title) || header == null) return (string.Empty, string.Empty, null);
        var (name, slug) = ClassesExtractor.ParseNameAndSlug(title);
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = ClassesExtractor.SlugMap.GetValueOrDefault(name)
                ?? SlugService.From(name, cacheKey: name);
        }
        return (name, slug, header);
    }
}
