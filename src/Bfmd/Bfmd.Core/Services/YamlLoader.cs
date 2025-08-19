using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Bfmd.Core.Services;

public class YamlLoader<T> : IYamlLoader<T>
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public T Load(string yamlPath)
    {
        var text = File.ReadAllText(yamlPath);
        return _deserializer.Deserialize<T>(text);
    }
}

