using Lathe.Core.Features.Sources;
using Lathe.Core.Features.Tailing;
using Lathe.Tool.Infrastructure;
using Lathe.Web;
using Lathe.Web.Features.Session;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net;
using System.Net.Sockets;

namespace Lathe.Tool.Features.Serve;

public sealed class ServeCommand(SourceResolver sourceResolver, TailService tailService)
    : AsyncCommand<ServeCommandSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ServeCommandSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            if (settings.Count <= 0)
            {
                AnsiConsole.MarkupLine("[red]`--count` must be greater than zero.[/]");
                return -1;
            }

            using var commandCancellation = ConsoleCancellation.Create(cancellationToken);
            var url = $"http://localhost:{GetOpenPort()}";
            var session = new LatheSessionState([], [], DateTimeOffset.UtcNow, settings.Count);
            var controller = new ServeSessionController(
                sourceResolver,
                tailService,
                session,
                Environment.CurrentDirectory,
                settings.Count,
                commandCancellation.Token);

            if (settings.Files.Length > 0 || settings.Globs.Length > 0 || settings.RollingDirectories.Length > 0)
            {
                var addErrors = await controller.AddInputsAsync(settings.Files, settings.Globs, settings.RollingDirectories, commandCancellation.Token);

                if (addErrors.Count > 0)
                {
                    foreach (var error in addErrors)
                    {
                        AnsiConsole.MarkupLine($"[red]{Markup.Escape(error)}[/]");
                    }

                    return -1;
                }
            }

            AnsiConsole.MarkupLine($"[green]Lathe is listening on[/] [link={url}]{url}[/]");
            AnsiConsole.MarkupLine($"[grey]Loaded {session.Events.Count} events from {session.Sources.Count} source(s). Add more in the UI and press Ctrl+C to stop.[/]");

            await using var app = LatheWebApplication.Build([], new LatheWebApplicationOptions(url, session, controller));
            await app.StartAsync(commandCancellation.Token);

            try
            {
                await Task.Delay(Timeout.Infinite, commandCancellation.Token);
            }
            finally
            {
                await app.StopAsync(CancellationToken.None);
            }

            return 0;
        }
        catch (SourceResolutionException ex)
        {
            foreach (var error in ex.Errors)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(error)}[/]");
            }

            return -1;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
    }

    private static int GetOpenPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

}
