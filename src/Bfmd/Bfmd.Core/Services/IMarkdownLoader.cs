namespace Bfmd.Core.Services;

public interface IMarkdownLoader
{
    (string content, string sha256) Load(string path);
}

