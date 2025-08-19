using System.CommandLine;
using Bfmd.Cli.Composition;
using Bfmd.Cli.Infrastructure;
using Bfmd.Core.Config;
using Bfmd.Core.Pipeline;
using Bfmd.Core.Services;
using Microsoft.Extensions.Logging;

namespace Bfmd.Cli.Commands;

public static class ConvertCommand
{
    public static Command Build(App app)
    {
        var cmd = new Command("convert", "Run pipeline: parse, extract, validate, serialize");
        var inOpt = new Option<string>("--in", () => "input", "Input root");
        var outOpt = new Option<string>("--out", () => "output", "Output root");
        var cfgOpt = new Option<string>("--config", () => "config", "Config root");
        var typesOpt = new Option<string>("--types", () => "classes,backgrounds,lineages", "csv types");
        cmd.AddOption(inOpt); cmd.AddOption(outOpt); cmd.AddOption(cfgOpt); cmd.AddOption(typesOpt);
        cmd.SetHandler((string input, string output, string cfgRoot, string typesCsv, string v) =>
        {
            using var loggerFactory = App.CreateLoggerFactory(v);
            var log = loggerFactory.CreateLogger("convert");

            var yamlSources = new YamlLoader<SourcesConfig>();
            var yamlPipeline = new YamlLoader<PipelineConfig>();
            SourcesConfig sources; PipelineConfig pipe;
            try { sources = yamlSources.Load(Path.Combine(cfgRoot, "sources.yaml")); }
            catch (Exception ex) { log.LogError("Config error: {M}", ex.Message); Environment.ExitCode = 2; return; }
            try { pipe = yamlPipeline.Load(Path.Combine(cfgRoot, "pipeline.yaml")); }
            catch (Exception ex) { log.LogError("Config error: {M}", ex.Message); Environment.ExitCode = 2; return; }

            var allowed = typesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
            pipe.Steps = pipe.Steps.Where(s => allowed.Contains(s.Type)).ToList();

            var runner = new PipelineRunner(log, App.MarkdownLoader, App.CreateMapLoader(), App.Extractors);
            var code = runner.Run(pipe, sources, (input, output, cfgRoot));
            Environment.ExitCode = code;
        }, inOpt, outOpt, cfgOpt, typesOpt, Options.VerbosityOption());
        return cmd;
    }
}

