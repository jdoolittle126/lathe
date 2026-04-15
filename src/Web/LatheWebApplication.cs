using Lathe.Web.Components;
using Lathe.Web.Features.Session;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Lathe.Web;

public sealed record LatheWebApplicationOptions(string Url, LatheSessionState Session);

public static class LatheWebApplication
{
    public static WebApplication Build(string[] args, LatheWebApplicationOptions options)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(LatheWebApplication).Assembly.GetName().Name,
        });

        builder.WebHost.UseUrls(options.Url);
        builder.WebHost.UseStaticWebAssets();
        builder.Logging.ClearProviders();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddSingleton(options.Session);

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }
}
