using Spectre.Console.Cli;

namespace Lathe.Tool.Features.Serve;

public sealed class ServeCommand(WebWorkbenchRunner workbenchRunner)
    : AsyncCommand<ServeCommandSettings>
{
    protected override Task<int> ExecuteAsync(CommandContext context, ServeCommandSettings settings, CancellationToken cancellationToken) =>
        workbenchRunner.RunAsync(
            settings.Files,
            settings.Globs,
            settings.RollingDirectories,
            settings.Count,
            settings.Open,
            cancellationToken);
}
