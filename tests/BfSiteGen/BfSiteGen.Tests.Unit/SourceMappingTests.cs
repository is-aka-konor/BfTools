using BfSiteGen.Core.IO;
using Xunit;

namespace BfSiteGen.Tests.Unit;

public class SourceMappingTests
{
    [Fact]
    public void Reader_Deserializes_Src_Single_Source()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "testdata", Guid.NewGuid().ToString("N"));
        var talentsDir = Path.Combine(root, "data", "talents");
        Directory.CreateDirectory(talentsDir);
        File.WriteAllText(Path.Combine(talentsDir, "t.json"), "{" +
            "\n  \"slug\": \"t\",\n  \"name\": \"T\",\n  \"description\": \"desc\",\n  \"category\": \"Magical\",\n  \"src\": { \"abbr\": \"BF\", \"name\": \"Black Flag\" }\n}" );

        var reader = new ContentReader();
        var res = reader.LoadAll(root);
        Assert.Single(res.Talents);
        Assert.Equal("BF", res.Talents[0].Src.Abbr);
        Assert.Equal("Black Flag", res.Talents[0].Src.Name);
    }
}
