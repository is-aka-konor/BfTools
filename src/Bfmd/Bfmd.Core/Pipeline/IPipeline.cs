using Bfmd.Core.Config;

namespace Bfmd.Core.Pipeline;

public interface IPipeline
{
    int Run(PipelineConfig cfg, SourcesConfig sources, (string In, string Out, string Config) paths);
}

