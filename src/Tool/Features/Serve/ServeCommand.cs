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

            if (settings.Files.Length == 0 && settings.Globs.Length == 0 && settings.RollingDirectories.Length == 0)
            {
                AnsiConsole.MarkupLine("[red]At least one input is required. Use `--file`, `--glob`, or `--rolling`.[/]");
                return -1;
            }

            var request = new ResolveSourcesRequest(
                Environment.CurrentDirectory,
                settings.Files,
                settings.Globs,
                settings.RollingDirectories);

            var sources = sourceResolver.Resolve(request);

            using var commandCancellation = ConsoleCancellation.Create(cancellationToken);
            var result = await tailService.StartAsync(new TailRequest(sources, settings.Count, true), commandCancellation.Token);
            var url = $"http://localhost:{GetOpenPort()}";
            var session = new LatheSessionState(sources, result.InitialEvents, DateTimeOffset.UtcNow, settings.Count);

            AnsiConsole.MarkupLine($"[green]Lathe is listening on[/] [link={url}]{url}[/]");
            AnsiConsole.MarkupLine($"[grey]Loaded {result.InitialEvents.Count} events from {sources.Count} source(s). Following live updates. Press Ctrl+C to stop.[/]");

            await using var app = LatheWebApplication.Build([], new LatheWebApplicationOptions(url, session));
            await app.StartAsync(commandCancellation.Token);

            var livePump = PumpLiveEventsAsync(result.LiveEvents, session, commandCancellation.Token);

            try
            {
                await Task.Delay(Timeout.Infinite, commandCancellation.Token);
            }
            finally
            {
                await app.StopAsync(CancellationToken.None);
                await livePump;
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

    private static async Task PumpLiveEventsAsync(
        IAsyncEnumerable<Lathe.Core.Domain.LogEventRecord> liveEvents,
        LatheSessionState session,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var logEvent in liveEvents.WithCancellation(cancellationToken))
            {
                session.Append(logEvent);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
