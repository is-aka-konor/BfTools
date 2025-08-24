using BfSiteGen.Core.IO;
using BfSiteGen.Core.Publishing;
using BfSiteGen.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

AnsiConsole.MarkupLine("[bold]BfSiteGen[/] — DTO → Static Site");

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();
builder.Services.AddSingleton<IContentReader, ContentReader>();
builder.Services.AddSingleton<SiteBundler>();
var host = builder.Build();

while (true)
{
    var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
        .Title("Select an action:")
        .AddChoices(new[] { "Build", "Publish", "Clean", "Exit" }));

    if (choice == "Exit") break;

    var inputArg = AnsiConsole.Prompt(new TextPrompt<string>("Input folder [grey](default: output)[/]").AllowEmpty());
    var distArg = AnsiConsole.Prompt(new TextPrompt<string>("Dist folder [grey](default: dist-site)[/]").AllowEmpty());
    string outputRoot = inputArg;
    string distRoot = distArg;

    #if DEBUG
    if (string.IsNullOrWhiteSpace(outputRoot)) outputRoot = "../../../../../../output";
    if (string.IsNullOrWhiteSpace(distRoot))  distRoot  = "../../../../../../dist-site";
    #else
    if (string.IsNullOrWhiteSpace(outputRoot)) outputRoot = "output";
    if (string.IsNullOrWhiteSpace(distRoot))  distRoot  = "dist-site";
    #endif

    if (choice == "Clean")
    {
        RunClean(distRoot);
        continue;
    }

    var publish = choice == "Publish";
    RunBuild(host.Services.GetRequiredService<SiteBundler>(), outputRoot, distRoot, publish);
}

static void RunBuild(SiteBundler bundler, string outputRoot, string distRoot, bool publish)
{
    AnsiConsole.MarkupLine($"[grey]Input:[/] [blue]{outputRoot}[/]  [grey]Dist:[/] [blue]{distRoot}[/]");
    if (!ValidateContentPresence(outputRoot))
    {
        AnsiConsole.MarkupLine("[red]No input JSON content found.[/]");
        AnsiConsole.MarkupLine($"Looked under: [blue]{Path.Combine(outputRoot, "data")}[/]");
        AnsiConsole.MarkupLine("Suggestion: run BFMD to extract DTOs:");
        AnsiConsole.MarkupLine("[grey]  dotnet run --project src/Bfmd/Bfmd.Cli --[/] then choose [bold]Convert[/].");
        return;
    }
    // Early SPA presence hint (pre-copy)
    var rootCwd = Directory.GetCurrentDirectory();
    var spaIndex = FindFileUpwards(rootCwd, Path.Combine("src","frontend","dist","index.html"));
    if (spaIndex is null)
    {
        AnsiConsole.MarkupLine("[yellow]SPA assets not found[/]: src/frontend/dist/index.html");
        AnsiConsole.MarkupLine("Suggestion: build the SPA assets:");
        AnsiConsole.MarkupLine("[grey]  cd src/frontend && npm ci && npm run build[/]");
    }
    AnsiConsole.Status()
        .Start("Building bundles...", _ =>
        {
            try
            {
                var res = bundler.Build(outputRoot, distRoot);
                foreach (var kv in res.Categories.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    var idx = res.Indexes.TryGetValue(kv.Key, out var ih) ? ih.hash : "-";
                    AnsiConsole.MarkupLine($"[green]•[/] {kv.Key}: hash=[blue]{kv.Value.hash}[/] index=[blue]{idx}[/] count=[yellow]{kv.Value.count}[/]");
                }
                // Copy frontend assets
                var root = Directory.GetCurrentDirectory();
                var frontendDist = FindFileUpwards(root, Path.Combine("src","frontend","dist","index.html"));
                if (frontendDist != null)
                {
                    var frontendDir = Path.GetDirectoryName(frontendDist)!;
                    AnsiConsole.MarkupLine($"[grey]Copy assets from[/] {frontendDir}");
                    CopyDirectory(frontendDir, distRoot);
                    var indexPath = Path.Combine(distRoot, "index.html");
                    if (File.Exists(indexPath))
                    {
                        var txt = File.ReadAllText(indexPath);
                        txt = txt.Replace(" src=\"/assets/", " src=\"assets/")
                                 .Replace(" href=\"/assets/", " href=\"assets/");
                        File.WriteAllText(indexPath, txt);
                        AnsiConsole.MarkupLine("[grey]Rewrote asset URLs in index.html[/]");
                    }
                    // Validate copied assets exist
                    if (!File.Exists(Path.Combine(distRoot, "index.html")) || !Directory.Exists(Path.Combine(distRoot, "assets")))
                    {
                        AnsiConsole.MarkupLine("[yellow]Warning[/]: SPA assets copy appears incomplete.");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Warning[/]: Frontend dist not found (src/frontend/dist). Skipping asset copy.");
                    AnsiConsole.MarkupLine("Suggestion: build the SPA assets:");
                    AnsiConsole.MarkupLine("[grey]  cd src/frontend && npm ci && npm run build[/]");
                }

                if (publish)
                {
                    var zipName = CreateZip(distRoot);
                    AnsiConsole.MarkupLine($"[green]ZIP created[/]: {zipName}");
                }

                AnsiConsole.MarkupLine($"[green]Done[/] → [blue]{distRoot}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Build failed[/]: {ex.Message}");
            }
        });
}

static void RunClean(string distRoot)
{
    if (string.IsNullOrWhiteSpace(distRoot)) distRoot = "dist-site";
    if (!Directory.Exists(distRoot))
    {
        AnsiConsole.MarkupLine($"[yellow]Nothing to clean[/]: '{distRoot}' not found.");
        return;
    }
    var confirm = AnsiConsole.Confirm($"Delete folder [red]{distRoot}[/]?", false);
    if (!confirm) return;
    try
    {
        Directory.Delete(distRoot, recursive: true);
        AnsiConsole.MarkupLine($"[green]Cleaned[/]: {distRoot}");
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Failed to clean[/]: {ex.Message}");
    }
}

static bool ValidateContentPresence(string outputRoot)
{
    try
    {
        var dataRoot = Path.Combine(outputRoot, "data");
        if (!Directory.Exists(dataRoot)) return false;
        return Directory.EnumerateFiles(dataRoot, "*.json", SearchOption.AllDirectories).Any();
    }
    catch { return false; }
}

static string? FindFileUpwards(string startDir, string relativePath)
{
    var dir = startDir;
    for (int i = 0; i < 8; i++)
    {
        var p = Path.Combine(dir, relativePath);
        if (File.Exists(p)) return p;
        var parent = Directory.GetParent(dir);
        if (parent == null) break;
        dir = parent.FullName;
    }
    return null;
}

static void CopyDirectory(string fromDir, string toDir)
{
    Directory.CreateDirectory(toDir);
    foreach (var file in Directory.GetFiles(fromDir, "*", SearchOption.AllDirectories))
    {
        var rel = Path.GetRelativePath(fromDir, file);
        var dest = Path.Combine(toDir, rel);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(file, dest, overwrite: true);
    }
}

static string CreateZip(string distRoot)
{
    var manifestPath = Path.Combine(distRoot, "site-manifest.json");
    var buildId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
    if (File.Exists(manifestPath))
    {
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(manifestPath));
        var build = doc.RootElement.TryGetProperty("build", out var b) ? b.GetString() : null;
        if (!string.IsNullOrWhiteSpace(build)) buildId = build!.Replace(":", "").Replace("-", "");
    }
    var zipName = $"site-bundle-{buildId}.zip";
    var zipDest = Path.Combine(Directory.GetCurrentDirectory(), zipName);
    if (File.Exists(zipDest)) File.Delete(zipDest);
    System.IO.Compression.ZipFile.CreateFromDirectory(distRoot, zipDest, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);
    return zipDest;
}
