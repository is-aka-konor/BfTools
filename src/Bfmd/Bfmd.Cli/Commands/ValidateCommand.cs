using System.CommandLine;
using Bfmd.Cli.Composition;
using Bfmd.Cli.Infrastructure;
using Bfmd.Core.Domain;
using Bfmd.Core.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Bfmd.Cli.Commands;

public static class ValidateCommand
{
    public static Command Build(App app)
    {
        var cmd = new Command("validate", "Re-validate JSON produced in output/data");
        var outOnly = new Option<string>("--out", () => "output", "Output root");
        cmd.AddOption(outOnly);
        cmd.SetHandler((string output, string v) =>
        {
            using var loggerFactory = App.CreateLoggerFactory(v);
            var log = loggerFactory.CreateLogger("validate");
            var errors = new List<string>();
            void ValidateFiles<T>(string type, IValidator<T> validator) where T : BaseEntity
            {
                var dir = Path.Combine(output, "data", type);
                if (!Directory.Exists(dir)) return;
                foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var obj = System.Text.Json.JsonSerializer.Deserialize<T>(json);
                        if (obj is null) { errors.Add($"{file}: failed to deserialize"); continue; }
                        var vr = validator.Validate(obj);
                        if (!vr.IsValid) errors.AddRange(vr.Errors.Select(e => $"{file}: {e.PropertyName} - {e.ErrorMessage}"));
                    }
                    catch (Exception ex) { errors.Add($"{file}: {ex.Message}"); }
                }
            }
            ValidateFiles("classes", new ClassDtoValidator());
            ValidateFiles("backgrounds", new BackgroundDtoValidator());
            ValidateFiles("lineages", new LineageDtoValidator());
            foreach (var e in errors) log.LogError("{e}", e);
            Environment.ExitCode = errors.Count == 0 ? 0 : 1;
        }, outOnly, Options.VerbosityOption());
        return cmd;
    }
}

