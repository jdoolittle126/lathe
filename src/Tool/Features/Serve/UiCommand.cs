using Spectre.Console;
using Spectre.Console.Cli;

namespace Lathe.Tool.Features.Serve;

public sealed class UiCommand(DesktopWorkbenchLauncher desktopWorkbenchLauncher, WebWorkbenchRunner workbenchRunner)
    : AsyncCommand<UiCommandSettings>
{
    protected override Task<int> ExecuteAsync(CommandContext context, UiCommandSettings settings, CancellationToken cancellationToken)
    {
        if (settings.Count <= 0)
        {
            AnsiConsole.MarkupLine("[red]`--count` must be greater than zero.[/]");
            return Task.FromResult(-1);
        }

        if (desktopWorkbenchLauncher.TryLaunch(
            settings.Files,
            settings.Globs,
            settings.RollingDirectories,
            settings.Count))
        {
            return Task.FromResult(0);
        }

        AnsiConsole.MarkupLine("[yellow]Desktop UI is not available; opening the browser UI instead.[/]");

        return workbenchRunner.RunAsync(
            settings.Files,
            settings.Globs,
            settings.RollingDirectories,
            settings.Count,
            true,
            cancellationToken);
    }
}
