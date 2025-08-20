using System.Text.Json;
using Bfmd.Core.Config;
using Bfmd.Core.Pipeline;
using Bfmd.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bfmd.IntegrationTests;

public class SpellsIntegrationTests
{
    [Fact]
    public void Pipeline_Extracts_Spells_And_Writes_Index()
    {
        var cwd = Directory.GetCurrentDirectory();
        var inputRoot = cwd; // copied input
        var outputRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var configRoot = Path.Combine(cwd, "config");

        var sources = new YamlLoader<SourcesConfig>().Load(Path.Combine(configRoot, "sources.yaml"));
        var pipe = new YamlLoader<PipelineConfig>().Load(Path.Combine(configRoot, "pipeline.yaml"));
        pipe.Steps = pipe.Steps.Where(s => string.Equals(s.Type, "spells", StringComparison.OrdinalIgnoreCase)).ToList();

        var runner = new PipelineRunner(NullLogger.Instance, new FileMarkdownLoader(), p => new YamlLoader<MappingConfig>().Load(p), [
            ("spells", new Bfmd.Extractors.SpellsExtractor())
        ]);
        var code = runner.Run(pipe, sources, (inputRoot, outputRoot, configRoot));
        Assert.Equal(0, code);

        var spellsDir = Path.Combine(outputRoot, "data", "spells");
        Assert.True(Directory.Exists(spellsDir));
        var idxPath = Path.Combine(outputRoot, "index", "spells.index.json");
        Assert.True(File.Exists(idxPath));

        // Check at least Dispel Magic and Divine Favor slugs exist
        var files = Directory.EnumerateFiles(spellsDir, "*.json").Select(Path.GetFileNameWithoutExtension).ToList();
        Assert.Contains("razveivanie-magii", files);
        
        var dispelPath = Path.Combine(spellsDir, "razveivanie-magii.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(dispelPath));
        var root = doc.RootElement;
        Assert.Equal("Развеивание магии", root.GetProperty("name").GetString());
        Assert.Equal(3, root.GetProperty("circle").GetInt32());
        Assert.True(root.TryGetProperty("effect", out var eff));
        Assert.True(eff.GetArrayLength() >= 1);
    }
}
