using Lathe.Web.Features.Session;

namespace Lathe.Web;

public class Program
{
    public static Task Main(string[] args)
    {
        var app = LatheWebApplication.Build(args, new LatheWebApplicationOptions("http://localhost:5000", LatheSessionState.Empty, DisabledLatheSessionController.Instance));
        return app.RunAsync();
    }
}
