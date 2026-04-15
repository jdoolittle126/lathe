using Lathe.Core.Domain;

namespace Lathe.Core.Features.Tailing;

public sealed record TailResult(
    IReadOnlyList<LogEventRecord> InitialEvents,
    IAsyncEnumerable<LogEventRecord> LiveEvents);
