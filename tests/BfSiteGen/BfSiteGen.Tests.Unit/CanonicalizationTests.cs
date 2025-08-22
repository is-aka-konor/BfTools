using System.Text.Json;
using BfSiteGen.Core.Models;
using BfSiteGen.Core.Publishing;
using Xunit;

namespace BfSiteGen.Tests.Unit;

public class CanonicalizationTests
{
    [Fact]
    public void Hash_Is_Deterministic_Across_Order()
    {
        var t1 = new Talent { Slug = "a", Name = "A", DescriptionMd = "md", DescriptionHtml = "<p>md</p>", Type = "Magical", Sources = [ new SourceRef { Abbr = "BF", Name = "Black Flag" } ] };
        var t2 = new Talent { Slug = "b", Name = "B", DescriptionMd = "md", DescriptionHtml = "<p>md</p>", Type = "Martial", Sources = [ new SourceRef { Abbr = "BF", Name = "Black Flag" } ] };

        var bytes1 = CanonicalJson.SerializeCanonicalArray(new[] { t2, t1 }, CanonicalJson.WriteCanonicalTalent);
        var bytes2 = CanonicalJson.SerializeCanonicalArray(new[] { t1, t2 }, CanonicalJson.WriteCanonicalTalent);
        var h1 = CanonicalJson.Sha256Hex(bytes1);
        var h2 = CanonicalJson.Sha256Hex(bytes2);
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void Hash_Changes_When_Content_Changes()
    {
        var t1 = new Talent { Slug = "a", Name = "A", DescriptionMd = "md1", DescriptionHtml = "<p>md1</p>", Type = "Magical", Sources = [ new SourceRef { Abbr = "BF", Name = "Black Flag" } ] };
        var t1b = new Talent { Slug = "a", Name = "A", DescriptionMd = "md2", DescriptionHtml = "<p>md2</p>", Type = "Magical", Sources = [ new SourceRef { Abbr = "BF", Name = "Black Flag" } ] };

        var h1 = CanonicalJson.Sha256Hex(CanonicalJson.SerializeCanonicalArray(new[] { t1 }, CanonicalJson.WriteCanonicalTalent));
        var h2 = CanonicalJson.Sha256Hex(CanonicalJson.SerializeCanonicalArray(new[] { t1b }, CanonicalJson.WriteCanonicalTalent));
        Assert.NotEqual(h1, h2);
    }
}

