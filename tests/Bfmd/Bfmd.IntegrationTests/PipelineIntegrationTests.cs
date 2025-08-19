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
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var input = Path.Combine(tmp, "input");
        var output = Path.Combine(tmp, "output");
        var config = Path.Combine(tmp, "config");
        Directory.CreateDirectory(Path.Combine(input, "classes"));
        Directory.CreateDirectory(Path.Combine(input, "backgrounds"));
        Directory.CreateDirectory(Path.Combine(input, "lineages"));
        Directory.CreateDirectory(config);

        File.WriteAllText(Path.Combine(input, "classes", "fighter.md"), "# Воин\nСводка.");
        File.WriteAllText(Path.Combine(input, "backgrounds", "acolyte.md"), "# Послушник\nОписание.");
        File.WriteAllText(Path.Combine(input, "lineages", "human.md"), "# Человек\nОписание.");

        File.WriteAllText(Path.Combine(config, "sources.yaml"), "sources:\n  - abbr: SRC\n    name: Source\n    version: '1'\n    inputRoot: " + input.Replace("\\", "/") + "\n");
        File.WriteAllText(Path.Combine(config, "pipeline.yaml"), "steps:\n  - type: classes\n    input: " + Path.Combine(input, "classes").Replace("\\", "/") + "\n    mapping: mapping.classes.yaml\n    outputData: /output/data/classes\n    outputIndex: /output/index/classes.index.json\n    enabled: true\n  - type: backgrounds\n    input: " + Path.Combine(input, "backgrounds").Replace("\\", "/") + "\n    mapping: mapping.backgrounds.yaml\n    outputData: /output/data/backgrounds\n    outputIndex: /output/index/backgrounds.index.json\n    enabled: true\n  - type: lineages\n    input: " + Path.Combine(input, "lineages").Replace("\\", "/") + "\n    mapping: mapping.lineages.yaml\n    outputData: /output/data/lineages\n    outputIndex: /output/index/lineages.index.json\n    enabled: true\n");
        File.WriteAllText(Path.Combine(config, "mapping.classes.yaml"), "titleHeaders: ['#']\n");
        File.WriteAllText(Path.Combine(config, "mapping.backgrounds.yaml"), "titleHeaders: ['#']\n");
        File.WriteAllText(Path.Combine(config, "mapping.lineages.yaml"), "titleHeaders: ['#']\n");

        var sources = new YamlLoader<SourcesConfig>().Load(Path.Combine(config, "sources.yaml"));
        var pipe = new YamlLoader<PipelineConfig>().Load(Path.Combine(config, "pipeline.yaml"));

        var runner = new PipelineRunner(NullLogger.Instance, new FileMarkdownLoader(), p => new YamlLoader<MappingConfig>().Load(p),
        [
            ("classes", (IExtractor)new Bfmd.Extractors.ClassesExtractor()),
            ("backgrounds", (IExtractor)new Bfmd.Extractors.BackgroundsExtractor()),
            ("lineages", (IExtractor)new Bfmd.Extractors.LineagesExtractor())
        ]);
        var code = runner.Run(pipe, sources, (input, output, config));
        Assert.Equal(0, code);
        Assert.True(File.Exists(Path.Combine(output, "manifest.json")));
    }
}

