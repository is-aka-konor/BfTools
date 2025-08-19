using System.Security.Cryptography;
using System.Text;

namespace Bfmd.Core.Services;

public class FileMarkdownLoader : IMarkdownLoader
{
    public (string content, string sha256) Load(string path)
    {
        using var fs = File.OpenRead(path);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(fs);
        fs.Position = 0;
        using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var content = sr.ReadToEnd();
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return (content, hex);
    }
}

