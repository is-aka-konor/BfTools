using BfSiteGen.Core.IO;
using BfSiteGen.Core.Publishing;
using BfSiteGen.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();
builder.Services.AddSingleton<IContentReader, ContentReader>();
builder.Services.AddSingleton<SiteBundler>();

var host = builder.Build();

var outputRoot = args.Length >= 1 ? args[0] : "output";
var distRoot = args.Length >= 2 ? args[1] : "dist-site";

var bundler = host.Services.GetRequiredService<SiteBundler>();
var res = bundler.Build(outputRoot, distRoot);

Console.WriteLine($"BfSiteGen bundles written to '{distRoot}'.");
foreach (var kv in res.Categories.OrderBy(k => k.Key, StringComparer.Ordinal))
{
    var idx = res.Indexes.TryGetValue(kv.Key, out var ih) ? ih.hash : "-";
    Console.WriteLine($" - {kv.Key}: hash={kv.Value.hash} index={idx} count={kv.Value.count}");
}
