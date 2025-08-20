using Bfmd.Core.Config;
using Bfmd.Core.Services;

namespace Bfmd.UnitTests;

public class ExtractorSmokeTests
{
    [Fact]
    public void ClassesExtractor_Basic()
    {
        const string md = "# Воин\n\nКраткое описание.";
        var doc = MarkdownAst.Parse(md);
        var src = new SourceItem { Abbr = "SRC", Name = "Src", Version = "1", InputRoot = "/input" };
        var map = new MappingConfig();
        var ex = new Extractors.ClassesExtractor();
        var res = ex.Extract([(path: "/input/classes/warrior.md", content: md, doc, sha256: "abc")], src, map).ToList();
        Assert.Single(res);
        Assert.Equal("Воин", res[0].Name);
    }
}
