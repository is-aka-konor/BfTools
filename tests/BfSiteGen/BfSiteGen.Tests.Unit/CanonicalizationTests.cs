using BfCommon.Domain.Models;
using BfSiteGen.Core.Publishing;
using Xunit;

namespace BfSiteGen.Tests.Unit;

public class CanonicalizationTests
{
    [Fact]
    public void Hash_Is_Deterministic_Across_Order()
    {
        var t1 = new TalentDto { Slug = "a", Name = "A", Description = "md", Category = "Magical", Src = new SourceRef { Abbr = "BF", Name = "Black Flag" } };
        var t2 = new TalentDto { Slug = "b", Name = "B", Description = "md", Category = "Martial", Src = new SourceRef { Abbr = "BF", Name = "Black Flag" } };

        var bytes1 = CanonicalJson.SerializeCanonicalArray(new[] { t2, t1 }, CanonicalJson.WriteCanonicalTalent);
        var bytes2 = CanonicalJson.SerializeCanonicalArray(new[] { t1, t2 }, CanonicalJson.WriteCanonicalTalent);
        var h1 = CanonicalJson.Sha256Hex(bytes1);
        var h2 = CanonicalJson.Sha256Hex(bytes2);
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void Hash_Changes_When_Content_Changes()
    {
        var t1 = new TalentDto { Slug = "a", Name = "A", Description = "md1", Category = "Magical", Src = new SourceRef { Abbr = "BF", Name = "Black Flag" } };
        var t1b = new TalentDto { Slug = "a", Name = "A", Description = "md2", Category = "Magical", Src = new SourceRef { Abbr = "BF", Name = "Black Flag" } };

        var h1 = CanonicalJson.Sha256Hex(CanonicalJson.SerializeCanonicalArray(new[] { t1 }, CanonicalJson.WriteCanonicalTalent));
        var h2 = CanonicalJson.Sha256Hex(CanonicalJson.SerializeCanonicalArray(new[] { t1b }, CanonicalJson.WriteCanonicalTalent));
        Assert.NotEqual(h1, h2);
    }
}
