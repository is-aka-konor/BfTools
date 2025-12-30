using System.IO.Compression;
using Bfmd.Core.Config;
using Bfmd.Core.Pipeline;
using Bfmd.Core.Services;
using Spectre.Console;
using Microsoft.Extensions.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .WriteTo.Console()
    .WriteTo.File("logs/bfmd.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateLogger();
AppDomain.CurrentDomain.ProcessExit += (_, _) => Log.CloseAndFlush();

AnsiConsole.MarkupLine("[bold]BFMD[/] — Markdown → JSON");

if (args.Length > 0)
{
    return RunCommand(args);
}

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
    var cfgRoot = "config";
    var resolvedOutput = ResolveOutputDefault(output);
    RunConvertWithOptions(input, resolvedOutput, cfgRoot);
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
    foreach (var t in new[] { "classes", "subclasses", "backgrounds", "lineages", "talents" }) CheckDir(t);
    if (errors.Count == 0) AnsiConsole.MarkupLine("[green]Validation passed[/]");
    else { AnsiConsole.MarkupLine($"[red]{errors.Count} errors[/]"); foreach (var e in errors) AnsiConsole.WriteLine(e); }
}

int RunCommand(string[] args)
{
    var command = args[0].ToLowerInvariant();
    var input = GetArg(args, "--input") ?? "input";
    var output = GetArg(args, "--output") ?? ResolveOutputDefault(string.Empty);
    var configRoot = GetArg(args, "--config") ?? "config";

    switch (command)
    {
        case "convert":
            return RunConvertWithOptions(input, output, configRoot);
        default:
            Console.WriteLine($"Unknown command '{args[0]}'. Supported: convert");
            return 1;
    }
}

int RunConvertWithOptions(string input, string output, string cfgRoot)
{
    var required = new[] { "sources.yaml", "pipeline.yaml" };
    foreach (var f in required)
    {
        if (!File.Exists(Path.Combine(cfgRoot, f)))
        {
            AnsiConsole.MarkupLine($"[red]Missing {cfgRoot}/{f}[/]");
            return 1;
        }
    }

    var sources = new YamlLoader<SourcesConfig>().Load(Path.Combine(cfgRoot, "sources.yaml"));
    var pipe = new YamlLoader<PipelineConfig>().Load(Path.Combine(cfgRoot, "pipeline.yaml"));

    // Clear output to avoid stale JSON confusing developers/testers.
    try
    {
        if (Directory.Exists(output))
        {
            Directory.Delete(output, true);
            Console.WriteLine("The output folder was deleted.");
        }
        Directory.CreateDirectory(output);
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Failed to clear output folder[/]: {ex.Message}");
        return 1;
    }

    using var lf = LoggerFactory.Create(b => { b.AddConsole(); b.SetMinimumLevel(LogLevel.Information); });
    var log = lf.CreateLogger("convert");
    var runner = new PipelineRunner(log, new FileMarkdownLoader(),
        p => new YamlLoader<MappingConfig>().Load(p), [
        ("classes", new Bfmd.Extractors.ClassesExtractor()),
        ("subclasses", new Bfmd.Extractors.SubclassesExtractor()),
        ("backgrounds", new Bfmd.Extractors.BackgroundsExtractor()),
        ("lineages", new Bfmd.Extractors.LineagesExtractor()),
        ("talents", new Bfmd.Extractors.TalentsExtractor()),
        ("spells", new Bfmd.Extractors.SpellsExtractor())
    ]);
    var code = runner.Run(pipe, sources, (input, output, cfgRoot));
    if (code == 0)
    {
        int Count(string folder)
        {
            var dir = Path.Combine(output, "data", folder);
            return Directory.Exists(dir) ? Directory.EnumerateFiles(dir, "*.json").Count() : 0;
        }
        var classes = Count("classes");
        var subclasses = Count("subclasses");
        var backgrounds = Count("backgrounds");
        var lineages = Count("lineages");
        var talents = Count("talents");
        var spells = Count("spells");
        AnsiConsole.MarkupLine($"[green]Done[/] → data: classes={classes}, subclasses={subclasses}, backgrounds={backgrounds}, lineages={lineages}, talents={talents}, spells={spells}");
        var manifest = Path.Combine(output, "manifest.json");
        if (File.Exists(manifest)) AnsiConsole.MarkupLine($"Manifest: [blue]{manifest}[/]");
    }
    else AnsiConsole.MarkupLine("[red]Failed[/]");
    return code;
}

static string ResolveOutputDefault(string output)
{
    #if DEBUG
    // In the Debug build, we want to save to the root of the repo.
    if (string.IsNullOrWhiteSpace(output)) return "../../../../../../output";
    #else
    if (string.IsNullOrWhiteSpace(output)) return "output";
    #endif
    return output;
}

static string? GetArg(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }
    return null;
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
