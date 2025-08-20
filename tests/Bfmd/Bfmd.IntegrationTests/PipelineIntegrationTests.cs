using Bfmd.Core.Config;
using Bfmd.Core.Pipeline;
using Bfmd.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bfmd.IntegrationTests;

public class PipelineIntegrationTests
{
    [Fact]
    public void Pipeline_WritesOutputs()
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

    // no-op helper removed; using test working directory with copied inputs/config
}
