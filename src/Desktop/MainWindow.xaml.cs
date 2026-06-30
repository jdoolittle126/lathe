using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using Lathe.Core.Features.Sources;
using Lathe.Core.Features.Tailing;
using Lathe.Core.Infrastructure.Logs;
using Lathe.Web;
using Lathe.Web.Features.Session;
using Microsoft.AspNetCore.Builder;

namespace Lathe.Desktop;

public partial class MainWindow : Window
{
    private const int InitialEventCount = 500;

    private readonly CancellationTokenSource _applicationCancellation = new();
    private readonly LatheSessionState _session = new([], [], DateTimeOffset.UtcNow, InitialEventCount);
    private Microsoft.AspNetCore.Builder.WebApplication? _webApplication;
    private DesktopSessionController? _sessionController;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var sourceResolver = new SourceResolver();
            var tailService = new TailService(new JsonLogEventParser());
            _sessionController = new DesktopSessionController(
                sourceResolver,
                tailService,
                _session,
                Environment.CurrentDirectory,
                InitialEventCount,
                _applicationCancellation.Token);

            var url = $"http://localhost:{GetOpenPort()}";
            _webApplication = LatheWebApplication.Build([], new LatheWebApplicationOptions(url, _session, _sessionController));
            await _webApplication.StartAsync(_applicationCancellation.Token);

            await Browser.EnsureCoreWebView2Async();
            Browser.Source = new Uri(url);
            SetStatus("Drop log files anywhere in this window to add them.");
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

    private static int GetOpenPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
