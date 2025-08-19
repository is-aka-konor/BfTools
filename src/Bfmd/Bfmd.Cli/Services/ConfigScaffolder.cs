using Microsoft.Extensions.Logging;

namespace Bfmd.Cli.Services;

public static class ConfigScaffolder
{
    public static void Scaffold(string configRoot, bool force, ILogger log)
    {
        Directory.CreateDirectory(configRoot);
        WriteIfMissing(Path.Combine(configRoot, "sources.yaml"), ConfigTemplates.SourcesYaml(), force, log);
        WriteIfMissing(Path.Combine(configRoot, "pipeline.yaml"), ConfigTemplates.PipelineYaml(), force, log);
        WriteIfMissing(Path.Combine(configRoot, "mapping.classes.yaml"), ConfigTemplates.MappingYaml(), force, log);
        WriteIfMissing(Path.Combine(configRoot, "mapping.backgrounds.yaml"), ConfigTemplates.MappingYaml(), force, log);
        WriteIfMissing(Path.Combine(configRoot, "mapping.lineages.yaml"), ConfigTemplates.MappingYaml(), force, log);
    }

    private static void WriteIfMissing(string path, string content, bool force, ILogger log)
    {
        if (File.Exists(path) && !force) { log.LogInformation("exists: {Path}", path); return; }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        log.LogInformation("created: {Path}", path);
    }
}

