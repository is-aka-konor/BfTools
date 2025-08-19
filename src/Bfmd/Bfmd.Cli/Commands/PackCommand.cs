using System.CommandLine;
using System.IO.Compression;
using Bfmd.Cli.Composition;
using Bfmd.Cli.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Bfmd.Cli.Commands;

public static class PackCommand
{
    public static Command Build(App app)
    {
        var cmd = new Command("pack", "Create a release zip with /data, /index, manifest.json");
        var outPack = new Option<string>("--out", () => "output", "Output root");
        var dest = new Option<string>("--dest", description: "Destination zip path") { IsRequired = true };
        cmd.AddOption(outPack); cmd.AddOption(dest);
        cmd.SetHandler((string output, string destZip, string v) =>
        {
            using var loggerFactory = App.CreateLoggerFactory(v);
            var log = loggerFactory.CreateLogger("pack");
            try
            {
                if (File.Exists(destZip)) File.Delete(destZip);
                using var archive = ZipFile.Open(destZip, ZipArchiveMode.Create);
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
            }
            catch (Exception ex) { log.LogError("pack failed: {m}", ex.Message); Environment.ExitCode = 3; }
        }, outPack, dest, Options.VerbosityOption());
        return cmd;
    }
}

