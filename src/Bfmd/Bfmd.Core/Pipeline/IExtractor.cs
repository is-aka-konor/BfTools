using Bfmd.Core.Config;
using Bfmd.Core.Domain;
using Markdig.Syntax;

namespace Bfmd.Core.Pipeline;

public interface IExtractor
{
    IEnumerable<BaseEntity> Extract(IEnumerable<(string path, MarkdownDocument doc, string sha256)> docs, SourceItem src, MappingConfig map);
}

