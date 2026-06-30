using Lathe.Core.Domain;

namespace Lathe.Web.Features.Session;

public sealed class LogPauseState
{
    private IReadOnlyList<LogEventRecord>? _pausedEvents;
    private HashSet<LogEventRecord>? _pausedEventSet;

    public bool IsPaused => _pausedEvents is not null;

    public IReadOnlyList<LogEventRecord> GetVisibleEvents(IReadOnlyList<LogEventRecord> currentEvents) =>
        _pausedEvents ?? currentEvents;

    public int GetWaitingEventCount(IReadOnlyList<LogEventRecord> currentEvents) =>
        _pausedEventSet is null
            ? 0
            : currentEvents.Count(logEvent => !_pausedEventSet.Contains(logEvent));

    public void Pause(IReadOnlyList<LogEventRecord> currentEvents)
    {
        _pausedEvents = currentEvents.ToArray();
        _pausedEventSet = new HashSet<LogEventRecord>(_pausedEvents);
    }

    public void Resume()
    {
        _pausedEvents = null;
        _pausedEventSet = null;
    }

    public void Toggle(IReadOnlyList<LogEventRecord> currentEvents)
    {
        if (IsPaused)
        {
            Resume();
            return;
        }

        Pause(currentEvents);
    }
}
