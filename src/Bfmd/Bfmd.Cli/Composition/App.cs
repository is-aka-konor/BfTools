using Bfmd.Cli.Infrastructure;
using Bfmd.Core.Config;
using Bfmd.Core.Pipeline;
using Bfmd.Core.Services;
using Microsoft.Extensions.Logging;

namespace Bfmd.Cli.Composition;

public class App
{
    public static ILoggerFactory CreateLoggerFactory(string verbosity) => LoggerFactoryProvider.Create(verbosity);

    public static IMarkdownLoader MarkdownLoader => new FileMarkdownLoader();

    public static Func<string, MappingConfig> CreateMapLoader() => path => new YamlLoader<MappingConfig>().Load(path);

    public static IEnumerable<(string key, IExtractor extractor)> Extractors =>
    [
        ("classes", new Bfmd.Extractors.ClassesExtractor()),
        ("backgrounds", new Bfmd.Extractors.BackgroundsExtractor()),
        ("lineages", new Bfmd.Extractors.LineagesExtractor())
    ];
}

