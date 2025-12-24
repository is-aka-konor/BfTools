using System.Text.Json;
using BfCommon.Domain.Models;
using BfSiteGen.Core.IO;
using BfSiteGen.Core.Publishing;
using NSubstitute;
using Xunit;

namespace BfSiteGen.Tests.Unit;

public class CanonicalWritersTests
{
    private static string NewTempDir() => Path.Combine(Path.GetTempPath(), "bfcanon-" + Guid.NewGuid().ToString("N"));

    private static JsonElement LoadFirstItem(string dist, string category, string hash)
    {
        var path = Path.Combine(dist, "data", $"{category}-{hash}.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        // Clone to detach from disposed JsonDocument
        return doc.RootElement.EnumerateArray().First().Clone();
    }

    private static void AssertAlphabeticalPropertyOrder(JsonElement obj)
    {
        var names = obj.EnumerateObject().Select(p => p.Name).ToList();
        var sorted = names.OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.Equal(sorted, names);
    }

    [Fact]
    public void Talent_Schema_NoMarkdown_Alphabetical_Order()
    {
        var load = new ContentLoadResult();
        load.Talents.Add(new TalentDto
        {
            Slug = "t1", Name = "Talent One", Category = "Magical",
            Description = "Intro.\n\nText.", Benefits = new() { "B1" },
            Src = new SourceRef { Abbr = "BF", Name = "Black Flag" }
        });

        var reader = Substitute.For<IContentReader>();
        reader.LoadAll(Arg.Any<string>()).Returns(load);
        var bundler = new SiteBundler(reader);
        var dist = NewTempDir();
        try
        {
            var res = bundler.Build("ignored", dist);
            var hash = res.Categories["talents"].hash;
            var first = LoadFirstItem(dist, "talents", hash);

            // Schema keys present
            Assert.True(first.TryGetProperty("benefits", out _));
            Assert.True(first.TryGetProperty("category", out _));
            Assert.True(first.TryGetProperty("description", out var desc));
            Assert.Contains("<p", desc.GetString());
            Assert.True(first.TryGetProperty("name", out _));
            Assert.True(first.TryGetProperty("slug", out _));
            Assert.True(first.TryGetProperty("sources", out var sources));
            Assert.True(sources.ValueKind == JsonValueKind.Array);
            // No markdown keys
            Assert.False(first.TryGetProperty("descriptionMd", out _));
            Assert.False(first.TryGetProperty("descriptionHtml", out _));
            // Alphabetical ordering
            AssertAlphabeticalPropertyOrder(first);
        }
        finally { try { Directory.Delete(dist, true); } catch { } }
    }

    [Fact]
    public void Spells_Schema_And_Effect_To_Paragraphs()
    {
        var load = new ContentLoadResult();
        load.Spells.Add(new SpellDto
        {
            Slug = "s1", Name = "Spell One", Circle = 1, School = "Evocation",
            CastingTime = "1 action", Range = "Self", Components = "V,S", Duration = "Instant",
            Effect = new() { "First.", "Second." },
            IsRitual = true,
            Src = new SourceRef { Abbr = "BF", Name = "Black Flag" }
        });

        var reader = Substitute.For<IContentReader>();
        reader.LoadAll(Arg.Any<string>()).Returns(load);
        var bundler = new SiteBundler(reader);
        var dist = NewTempDir();
        try
        {
            var res = bundler.Build("ignored", dist);
            var hash = res.Categories["spells"].hash;
            var first = LoadFirstItem(dist, "spells", hash);
            Assert.True(first.TryGetProperty("description", out var d));
            var html = d.GetString() ?? string.Empty;
            Assert.Contains("<p", html);
            Assert.Contains("First.", html);
            Assert.Contains("Second.", html);
            // Required keys present
            foreach (var k in new[] { "castingTime", "circle", "components", "duration", "name", "range", "school", "slug", "sources" })
                Assert.True(first.TryGetProperty(k, out _), $"missing {k}");
            Assert.True(first.TryGetProperty("isRitual", out var isRitual));
            Assert.True(isRitual.GetBoolean());
            // No markdown keys
            Assert.False(first.TryGetProperty("descriptionMd", out _));
            Assert.False(first.TryGetProperty("descriptionHtml", out _));
            AssertAlphabeticalPropertyOrder(first);
        }
        finally { try { Directory.Delete(dist, true); } catch { } }
    }

    [Fact]
    public void Class_Schema_And_Order()
    {
        var load = new ContentLoadResult();
        load.Classes.Add(new ClassDto
        {
            Slug = "c1", Name = "Class One", Description = "# Title", HitDie = "d10",
            SavingThrows = new() { "DEX", "CON" },
            Proficiencies = new ProficienciesDto { Skills = new SkillsPickDto { Choose = 1, From = new() { "Perception" } }, Armor = new() { "Light" }, Weapons = new() { "Simple" }, Tools = new() },
            StartingEquipment = new StartingEquipmentDto { Items = new() { "Pack" } },
            Levels = new() { new LevelRowDto { Level = 1, ProficiencyBonus = "+2", SpellSlots = new() { { 1, 2 } }, Features = new() { "Feat" } } },
            Features = new(), Subclasses = new(),
            Src = new SourceRef { Abbr = "BF", Name = "Black Flag" }
        });

        var reader = Substitute.For<IContentReader>();
        reader.LoadAll(Arg.Any<string>()).Returns(load);
        var bundler = new SiteBundler(reader);
        var dist = NewTempDir();
        try
        {
            var res = bundler.Build("ignored", dist);
            var hash = res.Categories["classes"].hash;
            var first = LoadFirstItem(dist, "classes", hash);
            foreach (var k in new[] { "description", "hitDie", "levels", "name", "proficiencies", "savingThrows", "slug", "sources", "startingEquipment" })
                Assert.True(first.TryGetProperty(k, out _), $"missing {k}");
            AssertAlphabeticalPropertyOrder(first);
        }
        finally { try { Directory.Delete(dist, true); } catch { } }
    }

    [Fact]
    public void Background_Schema_Includes_TalentDescription_Html()
    {
        var load = new ContentLoadResult();
        load.Backgrounds.Add(new BackgroundDto
        {
            Slug = "b1", Name = "BG", Description = "Paragraph.", TalentDescription = "Some *md*",
            SkillProficiencies = new SkillsPickDto { Choose = 1, From = new() { "Stealth" } },
            Languages = new SkillsPickDto(), ToolProficiencies = new SkillsPickDto(),
            Equipment = new() { "Item" }, Additional = new(), TalentOptions = new TalentOptionsDto { Choose = 1, From = new() { "Opt" } },
            Src = new SourceRef { Abbr = "BF", Name = "Black Flag" }
        });

        var reader = Substitute.For<IContentReader>();
        reader.LoadAll(Arg.Any<string>()).Returns(load);
        var bundler = new SiteBundler(reader);
        var dist = NewTempDir();
        try
        {
            var res = bundler.Build("ignored", dist);
            var hash = res.Categories["backgrounds"].hash;
            var first = LoadFirstItem(dist, "backgrounds", hash);
            Assert.True(first.TryGetProperty("talentDescription", out var td));
            Assert.Contains("<em>", td.GetString());
            foreach (var k in new[] { "description", "equipment", "languages", "name", "skillProficiencies", "slug", "sources", "talentOptions" })
                Assert.True(first.TryGetProperty(k, out _), $"missing {k}");
            AssertAlphabeticalPropertyOrder(first);
        }
        finally { try { Directory.Delete(dist, true); } catch { } }
    }

    [Fact]
    public void Lineage_Schema_And_Order()
    {
        var load = new ContentLoadResult();
        load.Lineages.Add(new LineageDto
        {
            Slug = "l1", Name = "Lin", Description = "Desc.", Size = "Средний", Speed = 30,
            Traits = new() { new TraitDto { Name = "Возраст", Description = "..." } },
            Src = new SourceRef { Abbr = "BF", Name = "Black Flag" }
        });

        var reader = Substitute.For<IContentReader>();
        reader.LoadAll(Arg.Any<string>()).Returns(load);
        var bundler = new SiteBundler(reader);
        var dist = NewTempDir();
        try
        {
            var res = bundler.Build("ignored", dist);
            var hash = res.Categories["lineages"].hash;
            var first = LoadFirstItem(dist, "lineages", hash);
            foreach (var k in new[] { "description", "name", "size", "slug", "sources", "speed", "traits" })
                Assert.True(first.TryGetProperty(k, out _), $"missing {k}");
            AssertAlphabeticalPropertyOrder(first);
        }
        finally { try { Directory.Delete(dist, true); } catch { } }
    }

    [Fact]
    public void Deterministic_Hash_Ignores_Input_Order()
    {
        var loadA = new ContentLoadResult();
        loadA.Talents.Add(new TalentDto { Slug = "b", Name = "B", Category = "Magical", Description = "md", Src = new SourceRef { Abbr = "BF", Name = "Black Flag" } });
        loadA.Talents.Add(new TalentDto { Slug = "a", Name = "A", Category = "Magical", Description = "md", Src = new SourceRef { Abbr = "BF", Name = "Black Flag" } });

        var readerA = Substitute.For<IContentReader>();
        readerA.LoadAll(Arg.Any<string>()).Returns(loadA);
        var bundlerA = new SiteBundler(readerA);
        var dist1 = NewTempDir();

        var loadB = new ContentLoadResult();
        loadB.Talents.Add(new TalentDto { Slug = "a", Name = "A", Category = "Magical", Description = "md", Src = new SourceRef { Abbr = "BF", Name = "Black Flag" } });
        loadB.Talents.Add(new TalentDto { Slug = "b", Name = "B", Category = "Magical", Description = "md", Src = new SourceRef { Abbr = "BF", Name = "Black Flag" } });
        var readerB = Substitute.For<IContentReader>();
        readerB.LoadAll(Arg.Any<string>()).Returns(loadB);
        var bundlerB = new SiteBundler(readerB);
        var dist2 = NewTempDir();

        try
        {
            var res1 = bundlerA.Build("ignored", dist1);
            var res2 = bundlerB.Build("ignored", dist2);
            Assert.Equal(res1.Categories["talents"].hash, res2.Categories["talents"].hash);
        }
        finally { try { Directory.Delete(dist1, true); } catch { } try { Directory.Delete(dist2, true); } catch { } }
    }
}
