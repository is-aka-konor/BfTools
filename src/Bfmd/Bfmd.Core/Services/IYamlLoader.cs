namespace Bfmd.Core.Services;

public interface IYamlLoader<T>
{
    T Load(string yamlPath);
}

