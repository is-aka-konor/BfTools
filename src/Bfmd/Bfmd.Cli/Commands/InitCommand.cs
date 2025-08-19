using System.CommandLine;
using Bfmd.Cli.Composition;
using Bfmd.Cli.Infrastructure;
using Bfmd.Cli.Services;

namespace Bfmd.Cli.Commands;

public static class InitCommand
{
    public static Command Build(App app)
    {
        var cmd = new Command("init", "Create default config skeletons under config/");
        var force = new Option<bool>("--force", () => false, "Overwrite existing files");
        var configRoot = new Option<string>("--config", () => "config", "Config root path");
        cmd.AddOption(force);
        cmd.AddOption(configRoot);
        cmd.SetHandler((bool isForced, string cfgRoot, string v) =>
        {
            using var loggerFactory = App.CreateLoggerFactory(v);
            var log = loggerFactory.CreateLogger("init");
            ConfigScaffolder.Scaffold(cfgRoot, isForced, log);
        }, force, configRoot, Options.VerbosityOption());
        return cmd;
    }
}

