using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using Lathe.Core.Features.Sources;
using Lathe.Core.Features.Tailing;
using Lathe.Core.Infrastructure.Logs;
using Lathe.Web;
using Lathe.Web.Features.Session;

namespace Lathe.Desktop;

public partial class MainWindow : Window
{
    private readonly DesktopLaunchOptions _options;
    private readonly CancellationTokenSource _applicationCancellation = new();
    private readonly LatheSessionState _session;
    private Microsoft.AspNetCore.Builder.WebApplication? _webApplication;
    private LiveLatheSessionController? _sessionController;

    public MainWindow(DesktopLaunchOptions options)
    {
        _options = options;
        _session = new LatheSessionState([], [], DateTimeOffset.UtcNow, options.Count);

        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var sourceResolver = new SourceResolver();
            var tailService = new TailService(new JsonLogEventParser());
            _sessionController = new LiveLatheSessionController(
                sourceResolver,
                tailService,
                _session,
                Environment.CurrentDirectory,
                _options.Count,
                _applicationCancellation.Token);

            var startupStatus = await LoadInitialInputsAsync();
            var url = $"http://localhost:{GetOpenPort()}";
            _webApplication = LatheWebApplication.Build([], new LatheWebApplicationOptions(url, _session, _sessionController));
            await _webApplication.StartAsync(_applicationCancellation.Token);

            await Browser.EnsureCoreWebView2Async();
            Browser.Source = new Uri(url);
            SetStatus(startupStatus);
        }
        catch (Exception ex)
        {
            SetStatus($"Desktop startup failed: {ex.Message}");
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop) || _sessionController is null)
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0)
        {
            return;
        }

        SetStatus($"Adding {paths.Length} dropped file(s)...");

        var errors = await _sessionController.AddInputsAsync(paths, [], [], CancellationToken.None);

        if (errors.Count > 0)
        {
            SetStatus(string.Join(Environment.NewLine, errors));
            return;
        }

        SetStatus($"Added {paths.Length} dropped file(s).");
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        _applicationCancellation.Cancel();

        if (_webApplication is not null)
        {
            await _webApplication.StopAsync(CancellationToken.None);
            await _webApplication.DisposeAsync();
        }

        _applicationCancellation.Dispose();
    }

    private void SetStatus(string message) => StatusText.Text = message;

    private async Task<string> LoadInitialInputsAsync()
    {
        if (!_options.HasInputs || _sessionController is null)
        {
            return "Drop log files anywhere in this window to add them.";
        }

        var errors = await _sessionController.AddInputsAsync(
            _options.Files,
            _options.Globs,
            _options.RollingDirectories,
            _applicationCancellation.Token);

        if (errors.Count > 0)
        {
            return string.Join(Environment.NewLine, errors);
        }

        return $"Loaded {_session.Sources.Count} source(s). Drop more log files anywhere in this window.";
    }

    private static int GetOpenPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
