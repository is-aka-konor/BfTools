using System.CommandLine;

namespace Bfmd.Cli.Infrastructure;

public static class Options
{
    public static Option<string> VerbosityOption()
        => new Option<string>("--verbosity", () => "normal", "quiet|minimal|normal|detailed");
}

