var builder = WebApplication.CreateBuilder(args);

// No server‑side views are needed for the demo; serve static SPA from wwwroot
var app = builder.Build();

// Static files from wwwroot (populated from dist-site at build/publish time)
var defaultFiles = new DefaultFilesOptions();
defaultFiles.DefaultFileNames.Clear();
defaultFiles.DefaultFileNames.Add("index.html");
app.UseDefaultFiles(defaultFiles);
app.UseStaticFiles();

// Note: All assets and JSON are served at root; no '/dist-site' mirror.

// Health/info endpoint to summarize served content
app.MapGet("/health-info", () =>
{
    var root = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    var manifestPath = Path.Combine(root, "site-manifest.json");
    var indexHtml = Path.Combine(root, "index.html");
    var sw = Path.Combine(root, "sw.js");
    var assetsDir = Path.Combine(root, "assets");
    var dataDir = Path.Combine(root, "data");
    var indexDir = Path.Combine(root, "index");

    bool manifestPresent = File.Exists(manifestPath);
    string? build = null;
    int sourcesCount = 0;
    Dictionary<string, object>? categories = null;
    try
    {
        if (manifestPresent)
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(manifestPath));
            var rootEl = doc.RootElement;
            build = rootEl.TryGetProperty("build", out var b) ? b.GetString() : null;
            if (rootEl.TryGetProperty("sources", out var s) && s.ValueKind == System.Text.Json.JsonValueKind.Array)
                sourcesCount = s.GetArrayLength();
            if (rootEl.TryGetProperty("categories", out var cats) && cats.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                categories = new();
                foreach (var p in cats.EnumerateObject())
                {
                    var v = p.Value;
                    categories[p.Name] = new
                    {
                        hash = v.TryGetProperty("hash", out var h) ? h.GetString() : null,
                        count = v.TryGetProperty("count", out var c) && c.TryGetInt32(out var ci) ? ci : 0,
                        indexHash = v.TryGetProperty("indexHash", out var ih) ? ih.GetString() : null
                    };
                }
            }
        }
    }
    catch { /* ignore parse errors; reported below */ }

    int CountFiles(string dir, string pattern) => Directory.Exists(dir) ? Directory.EnumerateFiles(dir, pattern, SearchOption.AllDirectories).Count() : 0;

    var report = new
    {
        ok = manifestPresent && File.Exists(indexHtml) && Directory.Exists(assetsDir),
        build,
        manifestPresent,
        indexHtmlPresent = File.Exists(indexHtml),
        serviceWorkerPresent = File.Exists(sw),
        assetsPresent = Directory.Exists(assetsDir),
        dataBundles = CountFiles(dataDir, "*.json"),
        indexFiles = CountFiles(indexDir, "*.json"),
        sourcesCount,
        categories
    };

    return Results.Json(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
});

// Fallback to index.html for unknown routes (SPA deep-links)
app.MapFallbackToFile("index.html");

app.Run();
