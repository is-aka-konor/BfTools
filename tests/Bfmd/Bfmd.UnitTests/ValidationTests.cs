using Bfmd.Core.Domain;
using Bfmd.Core.Validation;

namespace Bfmd.UnitTests;

public class ValidationTests
{
    [Fact]
    public void ClassValidator_ShouldFail_WhenHitDieInvalid()
    {
        var c = new ClassDto
        {
            Type = "class", Name = "Test", Slug = "test", Id = "SRC:class/test",
            Src = new SourceRef { Abbr = "SRC", Name = "S", Version = "1", Hash = "x" },
            HitDie = "d20",
            SavingThrows = ["Str", "Dex"],
            Levels = Enumerable.Range(1, 20).Select(i => new LevelRowDto { Level = i, ProficiencyBonus = "+2" }).ToList()
        };
        var vr = new ClassDtoValidator().Validate(c);
        Assert.False(vr.IsValid);
    }

    [Fact]
    public void BackgroundValidator_ShouldFail_WhenNoSkills()
    {
        var b = new BackgroundDto
        {
            Type = "background", Name = "B", Slug = "b", Id = "SRC:background/b",
            Src = new SourceRef { Abbr = "SRC", Name = "S", Version = "1", Hash = "x" },
            Concept = "c",
            SkillProficiencies = new SkillsPickDto { }
        };
        var vr = new BackgroundDtoValidator().Validate(b);
        Assert.False(vr.IsValid);
    }

    [Fact]
    public void LineageValidator_ShouldFail_WhenNoTraits()
    {
        var l = new LineageDto
        {
            Type = "lineage", Name = "L", Slug = "l", Id = "SRC:lineage/l",
            Src = new SourceRef { Abbr = "SRC", Name = "S", Version = "1", Hash = "x" },
            Size = "Medium", Speed = 30,
            Traits = []
        };
        var vr = new LineageDtoValidator().Validate(l);
        Assert.False(vr.IsValid);
    }
}
