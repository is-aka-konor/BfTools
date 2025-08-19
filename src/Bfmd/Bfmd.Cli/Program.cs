using System.CommandLine;
using Bfmd.Cli.Composition;
using Bfmd.Cli.Commands;
using Bfmd.Cli.Infrastructure;

var root = new RootCommand("bfmd - Markdown→JSON generator");
root.AddGlobalOption(Options.VerbosityOption());

var app = new App();
root.AddCommand(InitCommand.Build(app));
root.AddCommand(ConvertCommand.Build(app));
root.AddCommand(ValidateCommand.Build(app));
root.AddCommand(DiffCommand.Build(app));
root.AddCommand(PackCommand.Build(app));

return await root.InvokeAsync(args);

