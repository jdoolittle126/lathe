using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Lathe.Core.Features.Sources;
using Lathe.Core.Features.Tailing;
using Lathe.Tool.Infrastructure;
using Lathe.Web;
using Lathe.Web.Features.Session;
using Spectre.Console;

namespace Lathe.Tool.Features.Serve;

public sealed class WebWorkbenchRunner(SourceResolver sourceResolver, TailService tailService)
{
    public async Task<int> RunAsync(
        IReadOnlyList<string> files,
        IReadOnlyList<string> globs,
        IReadOnlyList<string> rollingDirectories,
        int count,
        bool openBrowser,
        CancellationToken cancellationToken)
    {
        try
        {
            if (count <= 0)
            {
                AnsiConsole.MarkupLine("[red]`--count` must be greater than zero.[/]");
                return -1;
            }

            using var commandCancellation = ConsoleCancellation.Create(cancellationToken);
            var url = $"http://localhost:{GetOpenPort()}";
            var session = new LatheSessionState([], [], DateTimeOffset.UtcNow, count);
            var controller = new ServeSessionController(
                sourceResolver,
                tailService,
                session,
                Environment.CurrentDirectory,
                count,
                commandCancellation.Token);

            if (files.Count > 0 || globs.Count > 0 || rollingDirectories.Count > 0)
            {
                var addErrors = await controller.AddInputsAsync(files, globs, rollingDirectories, commandCancellation.Token);

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

            if (openBrowser)
            {
                OpenBrowser(url);
            }

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

    private static void OpenBrowser(string url)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", url);
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", url);
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]Open {Markup.Escape(url)} in your browser.[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Could not open the browser automatically: {Markup.Escape(ex.Message)}[/]");
        }
    }

    private static int GetOpenPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
