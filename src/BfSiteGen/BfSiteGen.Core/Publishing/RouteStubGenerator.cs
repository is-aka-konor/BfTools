using BfSiteGen.Core.IO;

namespace BfSiteGen.Core.Publishing;

public static class RouteStubGenerator
{
    public static void Generate(ContentLoadResult load, string distRoot)
    {
        // Base category routes
        var bases = new[] { "spells", "talents", "classes", "lineages", "backgrounds", "intro", "spellcasting" };
        foreach (var b in bases)
            WriteStub(Path.Combine(distRoot, b, "index.html"));

        // Detail routes by slug
        foreach (var s in load.Spells) WriteStub(Path.Combine(distRoot, "spells", s.Slug, "index.html"));
        foreach (var t in load.Talents) WriteStub(Path.Combine(distRoot, "talents", t.Slug, "index.html"));
        foreach (var c in load.Classes) WriteStub(Path.Combine(distRoot, "classes", c.Slug, "index.html"));
        foreach (var l in load.Lineages) WriteStub(Path.Combine(distRoot, "lineages", l.Slug, "index.html"));
        foreach (var b in load.Backgrounds) WriteStub(Path.Combine(distRoot, "backgrounds", b.Slug, "index.html"));
    }

    private static void WriteStub(string path)
    {
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(path, StubHtml);
    }

    private const string StubHtml = "" +
        "<!doctype html>\n" +
        "<html lang=\"en\">\n" +
        "  <head>\n" +
        "    <meta charset=\"utf-8\"/>\n" +
        "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>\n" +
        "    <title>BfTools</title>\n" +
        "    <script>\n" +
        "    (function(){\n" +
        "      // Load main app assets from root index.html without changing the URL\n" +
        "      fetch('/index.html').then(function(r){return r.text()}).then(function(txt){\n" +
        "        var doc = new DOMParser().parseFromString(txt, 'text/html');\n" +
        "        var links = doc.querySelectorAll('link[rel=\\\'stylesheet\\\']');\n" +
        "        links.forEach(function(l){ document.head.appendChild(l.cloneNode(true)); });\n" +
        "        var scripts = doc.querySelectorAll('script[type=\\\'module\\\']');\n" +
        "        scripts.forEach(function(s){ var c=s.cloneNode(true); document.body.appendChild(c); });\n" +
        "      }).catch(function(e){ console.error('Failed to bootstrap SPA from stub', e); });\n" +
        "    })();\n" +
        "    </script>\n" +
        "  </head>\n" +
        "  <body>\n" +
        "    <div id=\"app\"><app-root></app-root></div>\n" +
        "  </body>\n" +
        "</html>\n";
}

