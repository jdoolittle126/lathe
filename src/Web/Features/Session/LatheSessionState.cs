using Lathe.Core.Domain;

namespace Lathe.Web.Features.Session;

public sealed class LatheSessionState
{
    private readonly object _gate = new();
    private readonly List<ResolvedLogSource> _sources;
    private readonly HashSet<string> _sourceKeys;
    private readonly List<LogEventRecord> _events;

    public LatheSessionState(
        IReadOnlyList<ResolvedLogSource> sources,
        IReadOnlyList<LogEventRecord> events,
        DateTimeOffset loadedAt,
        int windowSize,
        string? loadError = null)
    {
        _sources = [.. sources];
        _sourceKeys = new HashSet<string>(sources.Select(GetSourceKey), StringComparer.OrdinalIgnoreCase);
        LoadedAt = loadedAt;
        WindowSize = windowSize;
        LoadError = loadError;
        _events = events.OrderBy(logEvent => logEvent.Timestamp).TakeLast(windowSize).ToList();
        LastUpdatedAt = loadedAt;
    }

    public static LatheSessionState Empty { get; } = new([], [], DateTimeOffset.UtcNow, 100);

    public event Action? Changed;

    public IReadOnlyList<ResolvedLogSource> Sources
    {
        get
        {
            lock (_gate)
            {
                return _sources.ToArray();
            }
        }
    }

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
        AppendRange([logEvent]);
    }

    public IReadOnlyList<ResolvedLogSource> TryAddSources(IEnumerable<ResolvedLogSource> sources)
    {
        List<ResolvedLogSource> added = [];

        lock (_gate)
        {
            foreach (var source in sources)
            {
                if (!_sourceKeys.Add(GetSourceKey(source)))
                {
                    continue;
                }

                _sources.Add(source);
                added.Add(source);
            }

            if (added.Count > 0)
            {
                LastUpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        if (added.Count > 0)
        {
            Changed?.Invoke();
        }

        return added;
    }

    public void AppendRange(IEnumerable<LogEventRecord> events)
    {
        var appended = false;

        lock (_gate)
        {
            foreach (var logEvent in events)
            {
                _events.Add(logEvent);
                appended = true;
            }

            if (!appended)
            {
                return;
            }

            _events.Sort(static (left, right) => left.Timestamp.CompareTo(right.Timestamp));

            if (_events.Count > WindowSize)
            {
                _events.RemoveRange(0, _events.Count - WindowSize);
            }

            LastUpdatedAt = DateTimeOffset.UtcNow;
        }

        Changed?.Invoke();
    }

    private static string GetSourceKey(ResolvedLogSource source) =>
        source.Kind == LogSourceKind.RollingDirectory
            ? $"rolling:{source.RollingDirectoryPath ?? source.Path}"
            : $"file:{source.Path}";
}
