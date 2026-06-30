using Spectre.Console.Cli;

namespace Lathe.Tool.Features.Serve;

public class WebWorkbenchCommandSettings : CommandSettings
{
    [CommandOption("--file <PATH>")]
    public string[] Files { get; init; } = [];

    [CommandOption("--glob <PATTERN>")]
    public string[] Globs { get; init; } = [];

    [CommandOption("--rolling <DIRECTORY>")]
    public string[] RollingDirectories { get; init; } = [];

    [CommandOption("-n|--count <COUNT>")]
    public int Count { get; init; } = 100;
}

public sealed class ServeCommandSettings : WebWorkbenchCommandSettings
{
    [CommandOption("--open")]
    public bool Open { get; init; }
}

public sealed class UiCommandSettings : WebWorkbenchCommandSettings
{
}
