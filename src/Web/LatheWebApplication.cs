using Lathe.Web.Components;
using Lathe.Web.Features.Session;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lathe.Web;

public sealed record LatheWebApplicationOptions(string Url, LatheSessionState Session, ILatheSessionController? SessionController = null);

public static class LatheWebApplication
{
    public static WebApplication Build(string[] args, LatheWebApplicationOptions options)
    {
        var contentRoot = AppContext.BaseDirectory;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(LatheWebApplication).Assembly.GetName().Name,
            ContentRootPath = contentRoot,
            WebRootPath = Path.Combine(contentRoot, "wwwroot"),
        });

        builder.WebHost.UseUrls(options.Url);
        builder.Logging.ClearProviders();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddSingleton(options.Session);
        builder.Services.AddSingleton(options.SessionController ?? DisabledLatheSessionController.Instance);

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }
}
