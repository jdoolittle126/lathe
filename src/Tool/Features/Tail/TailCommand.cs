using Lathe.Core.Features.Sources;
using Lathe.Core.Features.Tailing;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Lathe.Tool.Features.Tail;

public sealed class TailCommand(SourceResolver sourceResolver, TailService tailService, TailRenderer renderer)
    : AsyncCommand<TailCommandSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, TailCommandSettings settings, CancellationToken cancellationToken)
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
            var result = await tailService.StartAsync(new TailRequest(sources, settings.Count, settings.Follow), commandCancellation.Token);

            renderer.RenderHeader(sources, settings.Count, settings.Follow);

            if (result.InitialEvents.Count == 0)
            {
                renderer.RenderEmpty();
            }
            else
            {
                foreach (var logEvent in result.InitialEvents)
                {
                    renderer.RenderEvent(logEvent);
                }
            }

            if (!settings.Follow)
            {
                return 0;
            }

            await foreach (var logEvent in result.LiveEvents.WithCancellation(commandCancellation.Token))
            {
                renderer.RenderEvent(logEvent);
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

    private sealed class ConsoleCancellation : IDisposable
    {
        private readonly CancellationTokenSource _cts;

        private ConsoleCancellation(CancellationTokenSource cts)
        {
            _cts = cts;
            Console.CancelKeyPress += OnCancelKeyPress;
        }

        public CancellationToken Token => _cts.Token;

        public static ConsoleCancellation Create(CancellationToken cancellationToken) =>
            new(CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));

        public void Dispose()
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
            _cts.Dispose();
        }

        private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            _cts.Cancel();
        }
    }
}
