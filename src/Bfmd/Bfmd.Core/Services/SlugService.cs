using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace Bfmd.Core.Services;

public static class SlugService
{
    private static readonly ConcurrentDictionary<string, string> Cache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<char, string> Map = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d",
        ['е'] = "e", ['ё'] = "e", ['ж'] = "zh", ['з'] = "z", ['и'] = "i",
        ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n",
        ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t",
        ['у'] = "u", ['ф'] = "f", ['х'] = "h", ['ц'] = "c", ['ч'] = "ch",
        ['ш'] = "sh", ['щ'] = "shch", ['ъ'] = "", ['ы'] = "y", ['ь'] = "",
        ['э'] = "e", ['ю'] = "yu", ['я'] = "ya"
    };

    public static string From(string name) => From(name, cacheKey: name);

    public static string From(string name, string cacheKey)
    {
        return Cache.GetOrAdd(cacheKey, _ => Normalize(name));
    }

    private static string Normalize(string input)
    {
        var lower = input.Trim().ToLowerInvariant();
        var sb = new StringBuilder();
        foreach (var ch in lower)
        {
            if (Map.TryGetValue(ch, out var rep)) { sb.Append(rep); continue; }
            if (char.IsLetterOrDigit(ch)) { sb.Append(ch); continue; }
            if (ch is ' ' or '_' or '-' or '/' or '\\') { sb.Append('-'); continue; }
            // ignore punctuation
        }
        var collapsed = Regex.Replace(sb.ToString(), "-+", "-");
        return collapsed.Trim('-');
    }
}
