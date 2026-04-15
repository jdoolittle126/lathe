using Lathe.Core.Domain;
using Spectre.Console;

namespace Lathe.Tool.Features.Tail;

public sealed class TailRenderer
{
    public void RenderHeader(IReadOnlyList<ResolvedLogSource> sources, int count, bool follow)
    {
        var mode = follow ? "recent + live" : "recent";
        var sourceList = string.Join(", ", sources.Select(source => Markup.Escape(source.DisplayName)));

        AnsiConsole.Write(new Rule($"[grey]Lathe tail[/] [white]{mode}[/] [grey]({count} events)[/]")
        {
            Justification = Justify.Left,
            Border = BoxBorder.Rounded,
        });

        AnsiConsole.MarkupLine($"[grey]sources:[/] {sourceList}");
        AnsiConsole.WriteLine();
    }

    public void RenderEmpty() => AnsiConsole.MarkupLine("[grey]No log events turned up yet.[/]");

    public void RenderEvent(LogEventRecord logEvent)
    {
        AnsiConsole.Write(new Text(logEvent.Timestamp.LocalDateTime.ToString("HH:mm:ss.fff"), new Style(foreground: Color.Grey)));
        AnsiConsole.Write(new Text("  "));
        AnsiConsole.Write(LevelBadge(logEvent.Level));
        AnsiConsole.Write(new Text("  "));
        AnsiConsole.Write(new Text(logEvent.SourceAlias, new Style(foreground: Color.Grey)));
        AnsiConsole.Write(new Text("  "));
        AnsiConsole.Write(new Text(logEvent.Message));
        AnsiConsole.WriteLine();

        if (!string.IsNullOrWhiteSpace(logEvent.Exception))
        {
            var exceptionText = new Text(logEvent.Exception.TrimEnd(), new Style(foreground: Color.Grey));
            AnsiConsole.Write(new Padder(exceptionText, new Padding(4, 0, 0, 0)));
            AnsiConsole.WriteLine();
        }
    }

    private static Text LevelBadge(string level)
    {
        var normalized = string.IsNullOrWhiteSpace(level) ? "Information" : level.Trim();
        var style = normalized.ToUpperInvariant() switch
        {
            "VERBOSE" => new Style(foreground: Color.Grey),
            "DEBUG" => new Style(foreground: Color.Blue),
            "INFORMATION" => new Style(foreground: Color.Green),
            "WARNING" => new Style(foreground: Color.Yellow),
            "ERROR" => new Style(foreground: Color.Red),
            "FATAL" => new Style(foreground: Color.White, background: Color.Red),
            _ => new Style(foreground: Color.Grey),
        };

        return new Text(normalized[..Math.Min(normalized.Length, 5)].PadRight(5), style with { Decoration = Decoration.Bold });
    }
}
