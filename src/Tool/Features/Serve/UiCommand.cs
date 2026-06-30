using Spectre.Console.Cli;

namespace Lathe.Tool.Features.Serve;

public sealed class UiCommand(WebWorkbenchRunner workbenchRunner)
    : AsyncCommand<UiCommandSettings>
{
    protected override Task<int> ExecuteAsync(CommandContext context, UiCommandSettings settings, CancellationToken cancellationToken) =>
        workbenchRunner.RunAsync(
            settings.Files,
            settings.Globs,
            settings.RollingDirectories,
            settings.Count,
            true,
            cancellationToken);
}
