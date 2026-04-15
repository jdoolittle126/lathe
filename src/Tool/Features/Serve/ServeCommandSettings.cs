using Spectre.Console.Cli;

namespace Lathe.Tool.Features.Serve;

public sealed class ServeCommandSettings : CommandSettings
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
