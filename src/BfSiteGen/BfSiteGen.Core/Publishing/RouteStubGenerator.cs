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
        "<html lang=\"en\" data-theme=\"tov\">\n" +
        "  <head>\n" +
        "    <meta charset=\"utf-8\"/>\n" +
        "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>\n" +
        "    <title>BfTools</title>\n" +
        "    <script>\n" +
        "    (function(){\n" +
        "      // Try to load app index.html from a list of candidate paths to support file:// and nested routes\n" +
        "      var candidates = ['../../index.html','../index.html','index.html','/index.html'];\n" +
        "      function tryNext(i){\n" +
        "        if(i>=candidates.length){ console.error('Failed to locate app index.html'); return; }\n" +
        "        fetch(candidates[i]).then(function(r){ if(!r.ok) throw new Error(r.status); return r.text().then(function(txt){ return { txt: txt, url: r.url }; }); }).then(function(payload){\n" +
        "        var doc = new DOMParser().parseFromString(payload.txt, 'text/html');\n" +
        "        var baseUrl = new URL(payload.url);\n" +
        "        // Apply theme from the real index.html if present\n" +
        "        var theme = (doc.documentElement && doc.documentElement.getAttribute('data-theme')) || (doc.body && doc.body.getAttribute('data-theme'));\n" +
        "        if(theme && !document.documentElement.getAttribute('data-theme')) document.documentElement.setAttribute('data-theme', theme);\n" +
        "        // Copy styles with absolute URLs resolved against the fetched index location\n" +
        "        var links = doc.querySelectorAll('link[rel=\\\'stylesheet\\\']');\n" +
        "        links.forEach(function(l){\n" +
        "          var href = l.getAttribute('href'); if(!href) return;\n" +
        "          var abs = new URL(href, baseUrl).toString();\n" +
        "          var tag = document.createElement('link'); tag.rel='stylesheet'; tag.href=abs;\n" +
        "          var cs = l.getAttribute('crossorigin'); if(cs) tag.setAttribute('crossorigin', cs);\n" +
        "          document.head.appendChild(tag);\n" +
        "        });\n" +
        "        // Copy module scripts with absolute URLs\n" +
        "        var scripts = doc.querySelectorAll('script[type=\\\'module\\\']');\n" +
        "        scripts.forEach(function(s){\n" +
        "          var src = s.getAttribute('src'); if(!src) return;\n" +
        "          var abs = new URL(src, baseUrl).toString();\n" +
        "          var tag = document.createElement('script'); tag.type='module'; tag.src=abs;\n" +
        "          var cs = s.getAttribute('crossorigin'); if(cs) tag.setAttribute('crossorigin', cs);\n" +
        "          document.body.appendChild(tag);\n" +
        "        });\n" +
        "      }).catch(function(){ tryNext(i+1); });\n" +
        "      }\n" +
        "      tryNext(0);\n" +
        "    })();\n" +
        "    </script>\n" +
        "  </head>\n" +
        "  <body>\n" +
        "    <div id=\"app\"><app-root></app-root></div>\n" +
        "  </body>\n" +
        "</html>\n";
}
