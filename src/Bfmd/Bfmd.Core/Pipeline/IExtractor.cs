using BfCommon.Domain.Models;
using Bfmd.Core.Config;
using Markdig.Syntax;

namespace Bfmd.Core.Pipeline;

public interface IExtractor
{
    IEnumerable<BaseEntity> Extract(IEnumerable<(string path, string content, MarkdownDocument doc, string sha256)> docs, SourceItem src, MappingConfig map);
}
