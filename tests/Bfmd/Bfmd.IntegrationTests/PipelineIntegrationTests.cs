using Bfmd.Core.Config;
using Bfmd.Core.Pipeline;
using Bfmd.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace Bfmd.IntegrationTests;

public class PipelineIntegrationTests
{
    [Fact]
    public void Pipeline_ShouldWriteOutputs_WhenRunningConfiguredPipeline()
    {
        var cwd = Directory.GetCurrentDirectory();
        var input = cwd; // pipeline uses relative paths like input/classes
        var output = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var config = Path.Combine(cwd, "config");

        var sources = new YamlLoader<SourcesConfig>().Load(Path.Combine(config, "sources.yaml"));
        var pipe = new YamlLoader<PipelineConfig>().Load(Path.Combine(config, "pipeline.yaml"));

        // Exclude spells step here; runner below doesn't register spells extractor
        pipe.Steps = pipe.Steps.Where(s => !string.Equals(s.Type, "spells", StringComparison.OrdinalIgnoreCase)).ToList();

        var runner = new PipelineRunner(NullLogger.Instance, new FileMarkdownLoader(), p => new YamlLoader<MappingConfig>().Load(p),
        [
            ("classes", new Extractors.ClassesExtractor()),
            ("backgrounds", new Extractors.BackgroundsExtractor()),
            ("lineages", new Extractors.LineagesExtractor())
        ]);
        var code = runner.Run(pipe, sources, (input, output, config));
        Assert.Equal(0, code);
        Assert.True(File.Exists(Path.Combine(output, "manifest.json")));
    }

    [Fact]
    public void Pipeline_ShouldExtractBackgrounds_FromMultipleSources()
    {
        var cwd = Directory.GetCurrentDirectory();
        var inputRoot = cwd;
        var outputRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var configRoot = Path.Combine(cwd, "config");

        var sources = new YamlLoader<SourcesConfig>().Load(Path.Combine(configRoot, "sources.yaml"));
        var pipe = new PipelineConfig
        {
            Steps =
            [
                new PipelineStepConfig
                {
                    Type = "backgrounds",
                    Input = "input/bfrd/backgrounds",
                    Mapping = "mapping.backgrounds.yaml",
                    Enabled = true
                },
                new PipelineStepConfig
                {
                    Type = "backgrounds",
                    Input = "input/tovpg1/backgrounds",
                    Mapping = "mapping.backgrounds.yaml",
                    Enabled = true
                }
            ]
        };

        var runner = new PipelineRunner(NullLogger.Instance, new FileMarkdownLoader(), p => new YamlLoader<MappingConfig>().Load(p),
        [
            ("backgrounds", new Extractors.BackgroundsExtractor())
        ]);

        try
        {
            var code = runner.Run(pipe, sources, (inputRoot, outputRoot, configRoot));
            Assert.Equal(0, code);

            var bgDir = Path.Combine(outputRoot, "data", "backgrounds");
            Assert.True(Directory.Exists(bgDir));
            var files = Directory.EnumerateFiles(bgDir, "*.json").ToList();
            Assert.NotEmpty(files);

            var sourcesFound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var root = doc.RootElement;
                if (root.TryGetProperty("src", out var src) && src.TryGetProperty("abbr", out var abbr))
                {
                    var value = abbr.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        sourcesFound.Add(value);
                }
            }

            Assert.Contains("BFRD", sourcesFound);
            Assert.Contains("ToVPG1", sourcesFound);
        }
        finally
        {
            try { Directory.Delete(outputRoot, true); } catch { }
        }
    }

    // no-op helper removed; using test working directory with copied inputs/config
}
