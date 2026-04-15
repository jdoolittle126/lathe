using Lathe.Core.Domain;

namespace Lathe.Web.Features.Session;

public sealed class LatheSessionState
{
    private readonly object _gate = new();
    private readonly List<LogEventRecord> _events;

    public LatheSessionState(
        IReadOnlyList<ResolvedLogSource> sources,
        IReadOnlyList<LogEventRecord> events,
        DateTimeOffset loadedAt,
        int windowSize,
        string? loadError = null)
    {
        Sources = sources;
        LoadedAt = loadedAt;
        WindowSize = windowSize;
        LoadError = loadError;
        _events = events.OrderBy(logEvent => logEvent.Timestamp).TakeLast(windowSize).ToList();
        LastUpdatedAt = loadedAt;
    }

    public static LatheSessionState Empty { get; } = new([], [], DateTimeOffset.UtcNow, 100);

    public event Action? Changed;

    public IReadOnlyList<ResolvedLogSource> Sources { get; }

    public DateTimeOffset LoadedAt { get; }

    public DateTimeOffset LastUpdatedAt { get; private set; }

    public int WindowSize { get; }

    public string? LoadError { get; }

    public IReadOnlyList<LogEventRecord> Events
    {
        get
        {
            lock (_gate)
            {
                return _events.ToArray();
            }
        }
    }

    public void Append(LogEventRecord logEvent)
    {
        lock (_gate)
        {
            _events.Add(logEvent);
            _events.Sort(static (left, right) => left.Timestamp.CompareTo(right.Timestamp));

            if (_events.Count > WindowSize)
            {
                _events.RemoveRange(0, _events.Count - WindowSize);
            }

            LastUpdatedAt = DateTimeOffset.UtcNow;
        }

        Changed?.Invoke();
    }
}
