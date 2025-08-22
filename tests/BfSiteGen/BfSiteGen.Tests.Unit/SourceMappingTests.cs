using BfSiteGen.Core.IO;
using BfSiteGen.Core.Services;
using Xunit;

namespace BfSiteGen.Tests.Unit;

public class SourceMappingTests
{
    [Fact]
    public void Reader_Maps_src_To_sources()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "testdata", Guid.NewGuid().ToString("N"));
        var talentsDir = Path.Combine(root, "data", "talents");
        Directory.CreateDirectory(talentsDir);
        File.WriteAllText(Path.Combine(talentsDir, "t.json"), "{" +
            "\n  \"slug\": \"t\",\n  \"name\": \"T\",\n  \"descriptionMd\": \"desc\",\n  \"type\": \"Magical\",\n  \"src\": { \"abbr\": \"BF\", \"name\": \"Black Flag\" }\n}" );

        var reader = new ContentReader(new MarkdownRenderer());
        var res = reader.LoadAll(root);
        Assert.Single(res.Talents);
        var t = res.Talents[0];
        Assert.Single(t.Sources);
        Assert.Equal("BF", t.Sources[0].Abbr);
        Assert.Equal("Black Flag", t.Sources[0].Name);
    }
}

