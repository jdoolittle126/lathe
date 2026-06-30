using System.Windows;

namespace Lathe.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var parseResult = DesktopLaunchOptions.Parse(e.Args);

        if (parseResult.Options is null)
        {
            MessageBox.Show(
                string.Join(Environment.NewLine, parseResult.Errors),
                "Lathe",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);
            return;
        }

        var window = new MainWindow(parseResult.Options);
        window.Show();
    }
}
