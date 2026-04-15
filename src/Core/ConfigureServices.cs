using Lathe.Core.Features.Sources;
using Lathe.Core.Features.Tailing;
using Lathe.Core.Infrastructure.Logs;
using Microsoft.Extensions.DependencyInjection;

namespace Lathe.Core;

public static class ConfigureServices
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<JsonLogEventParser>();
        services.AddSingleton<SourceResolver>();
        services.AddSingleton<TailService>();

        return services;
    }
}
