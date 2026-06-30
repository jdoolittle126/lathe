using Lathe.Core;
using Lathe.Tool.Features.Serve;
using Lathe.Tool.Features.Tail;
using Lathe.Tool.Infrastructure.Spectre;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

namespace Lathe.Tool;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddCoreServices();
        builder.Services.AddSingleton<TailCommand>();
        builder.Services.AddSingleton<ServeCommand>();
        builder.Services.AddSingleton<UiCommand>();
        builder.Services.AddSingleton<DesktopWorkbenchLauncher>();
        builder.Services.AddSingleton<WebWorkbenchRunner>();
        builder.Services.AddSingleton<TailRenderer>();

        var registrar = new TypeRegistrar(builder.Services);
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.SetApplicationName("lathe");
            config.PropagateExceptions();

            config.AddCommand<TailCommand>("tail")
                .WithDescription("Show recent JSON log events from one or more sources.");

            config.AddCommand<ServeCommand>("serve")
                .WithDescription("Host the local Lathe web UI for one or more sources.");

            config.AddCommand<UiCommand>("ui")
                .WithDescription("Open the Lathe desktop UI for one or more sources.");
        });

        return await app.RunAsync(args);
    }
}
