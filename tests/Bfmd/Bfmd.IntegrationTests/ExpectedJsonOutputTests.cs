using System.Text.Json;
using Bfmd.Core.Config;
using Bfmd.Core.Pipeline;
using Bfmd.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bfmd.IntegrationTests;

public class ExpectedJsonOutputTests
{
    [Fact]
    public void Convert_Emits_ClassJson_WithRequiredFields()
    {
        var cwd = Directory.GetCurrentDirectory();
        var inputRoot = cwd; // pipeline has relative inputs (copied into test output)
        var outputRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var configRoot = Path.Combine(cwd, "config");

        var sources = new YamlLoader<SourcesConfig>().Load(Path.Combine(configRoot, "sources.yaml"));
        var pipe = new YamlLoader<PipelineConfig>().Load(Path.Combine(configRoot, "pipeline.yaml"));
        // Filter pipeline to classes only for this test
        pipe.Steps = pipe.Steps.Where(s => string.Equals(s.Type, "classes", StringComparison.OrdinalIgnoreCase)).ToList();

        var runner = new PipelineRunner(NullLogger.Instance, new FileMarkdownLoader(), p => new YamlLoader<MappingConfig>().Load(p),
        [
            ("classes", new Extractors.ClassesExtractor())
        ]);

        var code = runner.Run(pipe, sources, (inputRoot, outputRoot, configRoot));
        Assert.Equal(0, code);

        var classesDir = Path.Combine(outputRoot, "data", "class");
        if (!Directory.Exists(classesDir))
        {
            classesDir = Path.Combine(outputRoot, "data", "classes");
        }

        Assert.True(Directory.Exists(classesDir), $"Missing classes output dir: {classesDir}");
        var files = Directory.EnumerateFiles(classesDir, "*.json").ToList();
        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("type", out var typeProp));
            Assert.Equal("class", typeProp.GetString());
            Assert.True(root.TryGetProperty("id", out var idProp));
            Assert.False(string.IsNullOrWhiteSpace(idProp.GetString()));
            Assert.True(root.TryGetProperty("slug", out var slugProp));
            Assert.False(string.IsNullOrWhiteSpace(slugProp.GetString()));
            Assert.True(root.TryGetProperty("name", out var nameProp));
            Assert.False(string.IsNullOrWhiteSpace(nameProp.GetString()));
            Assert.True(root.TryGetProperty("description", out var descProp));
            Assert.False(string.IsNullOrWhiteSpace(descProp.GetString()));
            if (root.TryGetProperty("hitDie", out var hd))
            {
                Assert.Contains(hd.GetString(), new[] { "d6", "d8", "d10", "d12" });
            }
        }

        // Spot-check Bard for proficiencies and skills parsing
        var bard = Path.Combine(classesDir, "bard.json");
        if (File.Exists(bard))
        {
            var json = File.ReadAllText(bard);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var prof = root.GetProperty("proficiencies");
            var skills = prof.GetProperty("skills");
            Assert.Equal(3, skills.GetProperty("choose").GetInt32());
            var from = skills.GetProperty("from").EnumerateArray().Select(e => e.GetString()).ToList();
            Assert.Contains("ANY", from);
            var armor = prof.GetProperty("armor").EnumerateArray().Select(e => e.GetString()).ToList();
            Assert.Contains(armor, a => a!.Contains("Лёгкие", StringComparison.OrdinalIgnoreCase));
            var weapons = prof.GetProperty("weapons").EnumerateArray().Select(e => e.GetString()).ToList();
            Assert.Contains(weapons, w => w!.Contains("Простое оружие", StringComparison.OrdinalIgnoreCase));
            var tools = prof.GetProperty("tools").EnumerateArray().Select(e => e.GetString()).ToList();
            Assert.NotEmpty(tools);
        }
    }

    [Fact]
    public void Convert_Emits_Background_WithExpectedFields()
    {
        var cwd = Directory.GetCurrentDirectory();
        var inputRoot = cwd;
        var outputRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var configRoot = Path.Combine(cwd, "config");

        var sources = new YamlLoader<SourcesConfig>().Load(Path.Combine(configRoot, "sources.yaml"));
        var pipe = new YamlLoader<PipelineConfig>().Load(Path.Combine(configRoot, "pipeline.yaml"));
        // Filter pipeline to backgrounds only for this test
        pipe.Steps = pipe.Steps.Where(s => string.Equals(s.Type, "backgrounds", StringComparison.OrdinalIgnoreCase)).ToList();

        var runner = new PipelineRunner(NullLogger.Instance, new FileMarkdownLoader(), p => new YamlLoader<MappingConfig>().Load(p),
        [
            ("backgrounds", new Extractors.BackgroundsExtractor())
        ]);

        var code = runner.Run(pipe, sources, (inputRoot, outputRoot, configRoot));
        Assert.Equal(0, code);
        var bgDir = Path.Combine(outputRoot, "data", "backgrounds");
        Assert.True(Directory.Exists(bgDir));
        var file = Path.Combine(bgDir, "prestupnik.json");
        Assert.True(File.Exists(file), "Expected prestupnik.json to be generated");

        var json = File.ReadAllText(file);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("background", root.GetProperty("type").GetString());
        Assert.Equal("prestupnik", root.GetProperty("slug").GetString());
        var skills = root.GetProperty("skillProficiencies");
        Assert.Equal(2, skills.GetProperty("choose").GetInt32());
        var from = skills.GetProperty("from").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("Скрытность", from);
        Assert.Contains("Обман", from);
        var talent = root.GetProperty("talentOptions");
        var tfrom = talent.GetProperty("from").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("Скрытный", tfrom);
        Assert.Contains("Дотошный", tfrom);
        Assert.Contains("Касание Удачи", tfrom);
        var desc = root.GetProperty("description").GetString();
        Assert.False(string.IsNullOrWhiteSpace(desc));
        var tdesc = root.GetProperty("talentDescription").GetString();
        Assert.Contains("Вы сводили концы", tdesc);
    }

    // no-op helper removed; using test working directory with copied inputs/config
}
