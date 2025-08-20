using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bfmd.Core.Services;

public static class JsonWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public static Task WriteAsync<T>(T obj, string path, CancellationToken ct = default)
        => WriteAsync((object?)obj!, path, ct);

    public static async Task WriteAsync(object obj, string path, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var fs = File.Create(path);
        await JsonSerializer.SerializeAsync(fs, obj, obj.GetType(), Options, ct);
    }

    public static string ToString<T>(T obj) => JsonSerializer.Serialize(obj, obj!.GetType(), Options);
}
