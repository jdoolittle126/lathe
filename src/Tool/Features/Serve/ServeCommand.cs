using Spectre.Console;
using Spectre.Console.Cli;

namespace Lathe.Tool.Features.Serve;

public sealed class ServeCommand : AsyncCommand
{
    protected override Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[yellow]`lathe serve` is next up.[/]");
        AnsiConsole.MarkupLine("[grey]The Blazor host is in the solution, but this first pass is all about getting `tail` solid.[/]");

        return Task.FromResult(0);
    }
}
