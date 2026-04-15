namespace Lathe.Tool.Infrastructure;

public sealed class ConsoleCancellation : IDisposable
{
    private readonly CancellationTokenSource _cts;

    private ConsoleCancellation(CancellationTokenSource cts)
    {
        _cts = cts;
        Console.CancelKeyPress += OnCancelKeyPress;
    }

    public CancellationToken Token => _cts.Token;

    public static ConsoleCancellation Create(CancellationToken cancellationToken) =>
        new(CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));

    public void Dispose()
    {
        Console.CancelKeyPress -= OnCancelKeyPress;
        _cts.Dispose();
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        _cts.Cancel();
    }
}
