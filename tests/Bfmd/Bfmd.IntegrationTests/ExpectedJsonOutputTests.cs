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
        // Resolve input root (prefer @input/classes if present)
        var repoRoot = Directory.GetCurrentDirectory();
        var atInput = Path.Combine(repoRoot, "@input");
        string inputRoot;
        if (Directory.Exists(atInput))
        {
            inputRoot = atInput;
        }
        else
        {
            // Fallback: synthesize minimal fixtures
            inputRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(inputRoot, "classes"));
            File.WriteAllText(Path.Combine(inputRoot, "classes", "fighter.md"), "# Воин\nОписание класса. d10 хит-дайс.");
        }

        var outputRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var configRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(configRoot);

        // Write minimal configs
        File.WriteAllText(Path.Combine(configRoot, "sources.yaml"), $"sources:\n  - abbr: SRC\n    name: Source\n    version: '1'\n    inputRoot: {inputRoot.Replace("\\", "/")}\n");
        File.WriteAllText(Path.Combine(configRoot, "pipeline.yaml"), $"steps:\n  - type: classes\n    input: {Path.Combine(inputRoot, "classes").Replace("\\", "/")}\n    mapping: mapping.classes.yaml\n    outputData: /output/data/classes\n    outputIndex: /output/index/classes.index.json\n    enabled: true\n");
        File.WriteAllText(Path.Combine(configRoot, "mapping.classes.yaml"), "titleHeaders: ['#']\n");

        var sources = new YamlLoader<SourcesConfig>().Load(Path.Combine(configRoot, "sources.yaml"));
        var pipe = new YamlLoader<PipelineConfig>().Load(Path.Combine(configRoot, "pipeline.yaml"));
        var runner = new PipelineRunner(NullLogger.Instance, new FileMarkdownLoader(), p => new YamlLoader<MappingConfig>().Load(p),
        [
            ("classes", (IExtractor)new Bfmd.Extractors.ClassesExtractor())
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
            if (root.TryGetProperty("hitDie", out var hd))
            {
                Assert.Contains(hd.GetString(), new[] { "d6", "d8", "d10", "d12" });
            }
        }
    }
}

