using Microsoft.Extensions.Logging;

namespace Bfmd.Cli.Infrastructure;

public static class LoggerFactoryProvider
{
    public static ILoggerFactory Create(string verbosity)
    {
        return Microsoft.Extensions.Logging.LoggerFactory.Create(b =>
        {
            b.AddConsole();
            b.SetMinimumLevel(verbosity switch
            {
                "quiet" => LogLevel.None,
                "minimal" => LogLevel.Warning,
                "detailed" => LogLevel.Debug,
                _ => LogLevel.Information
            });
        });
    }
}

