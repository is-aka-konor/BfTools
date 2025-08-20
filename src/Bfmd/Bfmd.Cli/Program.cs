using System.IO.Compression;
using Bfmd.Core.Config;
using Bfmd.Core.Pipeline;
using Bfmd.Core.Services;
using Spectre.Console;
using Microsoft.Extensions.Logging;

AnsiConsole.MarkupLine("[bold]BFMD[/] — Markdown → JSON");

while (true)
{
    var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
        .Title("Select an action:")
        .AddChoices(new[] { "Convert", "Validate", "Diff", "Pack", "Exit" }));

    switch (choice)
    {
        case "Convert":
            RunConvert();
            break;
        case "Validate":
            RunValidate();
            break;
        case "Diff":
            RunDiff();
            break;
        case "Pack":
            RunPack();
            break;
        default:
            return 0;
    }
}

void RunConvert()
{
    var input = AnsiConsole.Prompt(new TextPrompt<string>("Input folder [grey](default: input)[/]").AllowEmpty());
    if (string.IsNullOrWhiteSpace(input)) input = "input";
    var output = AnsiConsole.Prompt(new TextPrompt<string>("Output folder [grey](default: output)[/]").AllowEmpty());
    if (string.IsNullOrWhiteSpace(output)) output = "output";

    // Validate config presence
    var cfgRoot = "config";
    var required = new[] { "sources.yaml", "pipeline.yaml" };
    foreach (var f in required)
        if (!File.Exists(Path.Combine(cfgRoot, f))) { AnsiConsole.MarkupLine($"[red]Missing config/{f}[/]"); return; }

    var sources = new YamlLoader<SourcesConfig>().Load(Path.Combine(cfgRoot, "sources.yaml"));
    var pipe = new YamlLoader<PipelineConfig>().Load(Path.Combine(cfgRoot, "pipeline.yaml"));

    using var lf = LoggerFactory.Create(b => { b.AddConsole(); b.SetMinimumLevel(LogLevel.Information); });
    var log = lf.CreateLogger("convert");
    var runner = new PipelineRunner(log, new FileMarkdownLoader(), p => new YamlLoader<MappingConfig>().Load(p), new[]
    {
        ("classes", new Bfmd.Extractors.ClassesExtractor()),
        ("backgrounds", new Bfmd.Extractors.BackgroundsExtractor()),
        ("lineages", (IExtractor)new Bfmd.Extractors.LineagesExtractor()),
        ("spells", new Bfmd.Extractors.SpellsExtractor()),
    });
    var code = runner.Run(pipe, sources, (input, output, cfgRoot));
    if (code == 0)
    {
        int Count(string folder)
        {
            var dir = Path.Combine(output, "data", folder);
            return Directory.Exists(dir) ? Directory.EnumerateFiles(dir, "*.json").Count() : 0;
        }
        var classes = Count("classes");
        var backgrounds = Count("backgrounds");
        var lineages = Count("lineages");
        var spells = Count("spells");
        AnsiConsole.MarkupLine($"[green]Done[/] → data: classes={classes}, backgrounds={backgrounds}, lineages={lineages}, spells={spells}");
        var manifest = Path.Combine(output, "manifest.json");
        if (File.Exists(manifest)) AnsiConsole.MarkupLine($"Manifest: [blue]{manifest}[/]");
    }
    else AnsiConsole.MarkupLine("[red]Failed[/]");
}

void RunValidate()
{
    var output = AnsiConsole.Prompt(new TextPrompt<string>("Output folder [grey](default: output)[/]").AllowEmpty());
    if (string.IsNullOrWhiteSpace(output)) output = "output";

    // Simple JSON schema checks via validators in pipeline runner are handled at convert-time.
    // Here, just attempt re-parse and ensure required fields exist.
    var errors = new List<string>();
    void CheckDir(string type)
    {
        var dir = Path.Combine(output, "data", type);
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var doc = System.Text.Json.JsonDocument.Parse(json).RootElement;
                _ = doc.GetProperty("id"); _ = doc.GetProperty("slug"); _ = doc.GetProperty("name");
            }
            catch (Exception ex) { errors.Add($"{file}: {ex.Message}"); }
        }
    }
    foreach (var t in new[] { "classes", "backgrounds", "lineages" }) CheckDir(t);
    if (errors.Count == 0) AnsiConsole.MarkupLine("[green]Validation passed[/]");
    else { AnsiConsole.MarkupLine($"[red]{errors.Count} errors[/]"); foreach (var e in errors) AnsiConsole.WriteLine(e); }
}

void RunDiff()
{
    var since = AnsiConsole.Ask<string>("Path to previous output (folder with data/):");
    var output = AnsiConsole.Prompt(new TextPrompt<string>("Current output [grey](default: output)[/]").AllowEmpty());
    if (string.IsNullOrWhiteSpace(output)) output = "output";
    var now = Path.Combine(output, "data");
    var prev = Directory.Exists(since) ? Path.Combine(since, "data") : since;
    if (!Directory.Exists(prev)) { AnsiConsole.MarkupLine("[red]Invalid --since path[/]"); return; }
    var current = Directory.EnumerateFiles(now, "*.json", SearchOption.AllDirectories)
        .Select(f => (type: new DirectoryInfo(Path.GetDirectoryName(f)!).Name, slug: Path.GetFileNameWithoutExtension(f)))
        .ToHashSet();
    var previous = Directory.EnumerateFiles(prev, "*.json", SearchOption.AllDirectories)
        .Select(f => (type: new DirectoryInfo(Path.GetDirectoryName(f)!).Name, slug: Path.GetFileNameWithoutExtension(f)))
        .ToHashSet();
    var added = current.Except(previous).ToList();
    var removed = previous.Except(current).ToList();
    AnsiConsole.MarkupLine("[yellow]Added[/]: " + string.Join(", ", added.Select(a => $"{a.type}/{a.slug}")));
    AnsiConsole.MarkupLine("[yellow]Removed[/]: " + string.Join(", ", removed.Select(a => $"{a.type}/{a.slug}")));
}

void RunPack()
{
    var output = AnsiConsole.Prompt(new TextPrompt<string>("Output folder [grey](default: output)[/]").AllowEmpty());
    if (string.IsNullOrWhiteSpace(output)) output = "output";
    var dest = AnsiConsole.Ask<string>("Destination zip path:");
    try
    {
        if (File.Exists(dest)) File.Delete(dest);
        using var archive = ZipFile.Open(dest, ZipArchiveMode.Create);
        void AddDir(string relative)
        {
            var rootDir = Path.Combine(output, relative);
            if (!Directory.Exists(rootDir)) return;
            foreach (var file in Directory.EnumerateFiles(rootDir, "*", SearchOption.AllDirectories))
            {
                var entryName = Path.GetRelativePath(output, file).Replace('\\', '/');
                archive.CreateEntryFromFile(file, entryName);
            }
        }
        AddDir("data");
        AddDir("index");
        var manifest = Path.Combine(output, "manifest.json");
        if (File.Exists(manifest)) archive.CreateEntryFromFile(manifest, "manifest.json");
        AnsiConsole.MarkupLine("[green]Packed[/]");
    }
    catch (Exception ex) { AnsiConsole.MarkupLine($"[red]pack failed[/]: {ex.Message}"); }
}
