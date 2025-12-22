using System.Text;
using BfCommon.Domain.Models;
using Bfmd.Core.Config;
using Bfmd.Core.Services;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;

namespace Bfmd.Core.Pipeline;

public class PipelineRunner : IPipeline
{
    private readonly ILogger _log;
    private readonly IMarkdownLoader _mdLoader;
    private readonly Func<string, MappingConfig> _mapLoader;
    private readonly Dictionary<string, IExtractor> _extractors;

    public PipelineRunner(ILogger logger, IMarkdownLoader mdLoader, Func<string, MappingConfig> mapLoader, IEnumerable<(string key, IExtractor extractor)> extractors)
    {
        _log = logger;
        _mdLoader = mdLoader;
        _mapLoader = mapLoader;
        _extractors = extractors.ToDictionary(x => x.key, x => x.extractor, StringComparer.OrdinalIgnoreCase);
    }

    public int Run(PipelineConfig cfg, SourcesConfig sources, (string In, string Out, string Config) paths)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        var allEntities = new List<BaseEntity>();
        var totalOutputs = 0;
        foreach (var step in cfg.Steps.Where(s => s.Enabled))
        {
            // step handled by registered extractors
            if (!_extractors.TryGetValue(step.Type, out var extractor))
            {
                // Treat unknown step as a warning to allow partial pipelines
                warnings.Add($"Unknown step type '{step.Type}' — skipping");
                continue;
            }

            string inputPath;
            if (Path.IsPathRooted(step.Input)) inputPath = step.Input;
            else
            {
                var absStep = Path.GetFullPath(step.Input);
                var absIn = Path.GetFullPath(paths.In);
                inputPath = absStep.StartsWith(absIn, StringComparison.OrdinalIgnoreCase)
                    ? step.Input
                    : Path.Combine(paths.In, step.Input);
            }
            var files = Directory.Exists(inputPath)
                ? Directory.EnumerateFiles(inputPath, "*.md", SearchOption.AllDirectories)
                : Enumerable.Empty<string>();
            if (!files.Any())
                warnings.Add($"{step.Type}: no markdown files under '{inputPath}'");

            var src = sources.Sources.FirstOrDefault(s => Path.GetFullPath(inputPath).StartsWith(Path.GetFullPath(s.InputRoot), StringComparison.OrdinalIgnoreCase));
            if (src == null)
            {
                errors.Add($"No matching source for input '{inputPath}'");
                continue;
            }

            MappingConfig map;
            var mappingFile = string.IsNullOrWhiteSpace(step.Mapping)
                ? $"mapping.{step.Type}.yaml"
                : step.Mapping;
            try { map = _mapLoader(Path.Combine(paths.Config, mappingFile)); }
            catch (Exception ex) { errors.Add($"Failed to load mapping '{step.Mapping}': {ex.Message}"); continue; }

            var docs = new List<(string path, string content, MarkdownDocument doc, string sha256)>();
            foreach (var f in files)
            {
                try
                {
                    var (content, sha) = _mdLoader.Load(f);
                    var doc = MarkdownAst.Parse(content);
                    docs.Add((f, content, doc, sha));
                }
                catch (Exception ex)
                {
                    errors.Add($"{f}: failed to parse markdown: {ex.Message}");
                }
            }

            var entities = extractor.Extract(docs, src, map).ToList();
            if (docs.Count == 0)
                warnings.Add($"{step.Type}: parsed 0 documents");
            if (entities.Count == 0)
                warnings.Add($"{step.Type}: extracted 0 entities");

            // Assign IDs, slugs, and src, normalize common
            foreach (var e in entities)
            {
                e.Slug = string.IsNullOrWhiteSpace(e.Slug) 
                    ? SlugService.From(e.Name, cacheKey: e.Name) 
                    : e.Slug;
                e.Id = $"{src.Abbr}:{e.Type}/{e.Slug}";
                e.Src = new SourceRef
                {
                    Abbr = src.Abbr,
                    Name = src.Name,
                    Version = src.Version,
                    Url = src.Url,
                    License = src.License,
                    Hash = (e.SourceFile != null ? docs.FirstOrDefault(d => d.path.Equals(e.SourceFile, StringComparison.OrdinalIgnoreCase)).sha256 : null) ?? string.Empty
                };
            }

            // Validate per type (custom validators)
            foreach (var e in entities)
            {
                Validation.ValidationResult vr = e switch
                {
                    ClassDto c => new Validation.ClassDtoValidator().Validate(c),
                    BackgroundDto b => new Validation.BackgroundDtoValidator().Validate(b),
                    LineageDto l => new Validation.LineageDtoValidator().Validate(l),
                    _ => new Validation.ValidationResult()
                };
                if (!vr.IsValid)
                {
                    foreach (var f in vr.Errors) errors.Add($"{e.Type}/{e.Slug}: {f.PropertyName} - {f.ErrorMessage}");
                }
            }

            allEntities.AddRange(entities);

            // Serialize per-entity
            foreach (var e in entities)
            {
                try
                {
                    var typeDir = Path.Combine(paths.Out, "data", TypeToFolder(e.Type));
                    var path = Path.Combine(typeDir, $"{e.Slug}.json");
                    JsonWriter.WriteAsync(e, path).GetAwaiter().GetResult();
                    totalOutputs++;
                }
                catch (Exception ex) { errors.Add($"Serialize {e.Id}: {ex.Message}"); }
            }

            // Write per-type index (lightweight)
            try
            {
                var byType = entities.GroupBy(e => e.Type);
                foreach (var grp in byType)
                {
                    var list = grp.Select(e => new
                    {
                        e.Id,
                        e.Slug,
                        e.Name,
                        src = new { abbr = e.Src.Abbr, name = e.Src.Name },
                        e.Summary,
                        hitDie = (e as ClassDto)?.HitDie,
                        primaryAbilities = (e as ClassDto)?.PrimaryAbilities,
                        savingThrows = (e as ClassDto)?.SavingThrows,
                        concept = (e as BackgroundDto)?.Concept,
                        size = (e as LineageDto)?.Size,
                        speed = (e as LineageDto)?.Speed,
                        circle = (e as SpellDto)?.Circle,
                        school = (e as SpellDto)?.School
                    }).ToList();
                    var idxPath = Path.Combine(paths.Out, "index", $"{TypeToFolder(grp.Key)}.index.json");
                    JsonWriter.WriteAsync(list, idxPath).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex) { errors.Add($"Index write failed: {ex.Message}"); }
        }

        // Manifest
        try
        {
            var counts = new
            {
                classes = allEntities.Count(e => e is ClassDto),
                backgrounds = allEntities.Count(e => e is BackgroundDto),
                lineages = allEntities.Count(e => e is LineageDto),
                talents = allEntities.Count(e => e is TalentDto),
                spells = allEntities.Count(e => e is SpellDto)
            };
            var manifest = new
            {
                schemaVersion = "1.0.0",
                builtAtUtc = DateTime.UtcNow.ToString("o"),
                counts,
                sources = sources.Sources.Select(s => new { s.Abbr, s.Name, s.Version }).ToList(),
                warnings = warnings.Count,
                checksum = "sha256"
            };
            var mpath = Path.Combine(paths.Out, "manifest.json");
            JsonWriter.WriteAsync(manifest, mpath).GetAwaiter().GetResult();
        }
        catch (Exception ex) { errors.Add($"Manifest write failed: {ex.Message}"); }

        // Report
        try
        {
            Directory.CreateDirectory(Path.Combine(paths.Out));
            var logPath = Path.Combine(paths.Out, "report.log");
            var sb = new StringBuilder();
            sb.AppendLine($"Entities: {allEntities.Count}");
            sb.AppendLine($"Warnings: {warnings.Count}");
            foreach (var er in errors) sb.AppendLine("ERROR: " + er);
            File.WriteAllText(logPath, sb.ToString());
            var repPath = Path.Combine(paths.Out, "report.json");
            var report = new { errors, warnings, counts = new { total = allEntities.Count } };
            JsonWriter.WriteAsync(report, repPath).GetAwaiter().GetResult();
        }
        catch { /* ignore */ }

        if (allEntities.Count == 0 && totalOutputs == 0)
            errors.Add("Pipeline produced 0 entities — check input and mappings.");
        if (errors.Count > 0) return 1; // validation/IO generic failure
        return 0;
    }

    private static string TypeToFolder(string type) => type switch
    {
        "class" => "classes",
        "background" => "backgrounds",
        "lineage" => "lineages",
        "spell" => "spells",
        "talent" => "talents",
        _ => type
    };
}
