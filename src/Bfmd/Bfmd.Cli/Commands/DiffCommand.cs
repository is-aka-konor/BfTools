using System.CommandLine;
using Bfmd.Cli.Composition;
using Bfmd.Cli.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Bfmd.Cli.Commands;

public static class DiffCommand
{
    public static Command Build(App app)
    {
        var cmd = new Command("diff", "Compare two output folders (or manifest versions)");
        var since = new Option<string>("--since", description: "Path to previous output root or manifest.json folder");
        var outDiff = new Option<string>("--out", () => "output", "Current output root");
        cmd.AddOption(since); cmd.AddOption(outDiff);
        cmd.SetHandler((string sincePath, string output, string v) =>
        {
            using var loggerFactory = App.CreateLoggerFactory(v);
            var log = loggerFactory.CreateLogger("diff");
            var now = Path.Combine(output, "data");
            var prev = Directory.Exists(sincePath) ? Path.Combine(sincePath, "data") : sincePath;
            if (!Directory.Exists(prev)) { log.LogError("--since path invalid"); Environment.ExitCode = 2; return; }
            var current = Directory.EnumerateFiles(now, "*.json", SearchOption.AllDirectories)
                .Select(f => (type: new DirectoryInfo(Path.GetDirectoryName(f)!).Name, slug: Path.GetFileNameWithoutExtension(f)))
                .ToHashSet();
            var previous = Directory.EnumerateFiles(prev, "*.json", SearchOption.AllDirectories)
                .Select(f => (type: new DirectoryInfo(Path.GetDirectoryName(f)!).Name, slug: Path.GetFileNameWithoutExtension(f)))
                .ToHashSet();
            var added = current.Except(previous).ToList();
            var removed = previous.Except(current).ToList();
            Console.WriteLine($"Added: {string.Join(", ", added.Select(a => $"{a.type}/{a.slug}"))}");
            Console.WriteLine($"Removed: {string.Join(", ", removed.Select(a => $"{a.type}/{a.slug}"))}");
        }, since, outDiff, Options.VerbosityOption());
        return cmd;
    }
}

