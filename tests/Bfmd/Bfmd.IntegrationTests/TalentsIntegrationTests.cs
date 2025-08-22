using System.Text.Json;
using Bfmd.Core.Config;
using Bfmd.Core.Pipeline;
using Bfmd.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bfmd.IntegrationTests;

public class TalentsIntegrationTests
{
    [Fact]
    public void Pipeline_ShouldExtractTalentsAndWriteIndex_WhenRunningTalentsStep()
    {
        var cwd = Directory.GetCurrentDirectory();
        var inputRoot = cwd; // inputs are copied under test bin dir via csproj
        var outputRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var configRoot = Path.Combine(cwd, "config");

        var sources = new YamlLoader<SourcesConfig>().Load(Path.Combine(configRoot, "sources.yaml"));
        var pipe = new YamlLoader<PipelineConfig>().Load(Path.Combine(configRoot, "pipeline.yaml"));
        pipe.Steps = pipe.Steps.Where(s => string.Equals(s.Type, "talents", StringComparison.OrdinalIgnoreCase)).ToList();

        var runner = new PipelineRunner(NullLogger.Instance, new FileMarkdownLoader(), p => new YamlLoader<MappingConfig>().Load(p),
        [
            ("talents", new Extractors.TalentsExtractor())
        ]);

        var code = runner.Run(pipe, sources, (inputRoot, outputRoot, configRoot));
        Assert.Equal(0, code);

        var dir = Path.Combine(outputRoot, "data", "talents");
        Assert.True(Directory.Exists(dir));
        var files = Directory.EnumerateFiles(dir, "*.json").ToList();
        Assert.NotEmpty(files);

        var arkanist = Path.Combine(dir, "arkanist.json");
        if (File.Exists(arkanist))
        {
            var json = File.ReadAllText(arkanist);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.Equal("talent", root.GetProperty("type").GetString());
            Assert.Equal("arkanist", root.GetProperty("slug").GetString());
            Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("name").GetString()));
            Assert.True(root.TryGetProperty("description", out var descProp));
            Assert.Contains("Арканист", descProp.GetString());

            // Verify src.hash populated with a SHA-256 hex string
            var src = root.GetProperty("src");
            var hash = src.GetProperty("hash").GetString();
            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.Equal(64, hash!.Length);
            Assert.True(hash.All(ch => Uri.IsHexDigit(ch)));
        }
    }
}
