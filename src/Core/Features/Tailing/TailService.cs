using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Lathe.Core.Domain;
using Lathe.Core.Features.Sources;
using Lathe.Core.Infrastructure.Logs;

namespace Lathe.Core.Features.Tailing;

public sealed class TailService(JsonLogEventParser parser)
{
    public async Task<TailResult> StartAsync(TailRequest request, CancellationToken cancellationToken)
    {
        var snapshots = new List<TailSnapshot>(request.Sources.Count);

        foreach (var source in request.Sources)
        {
            snapshots.Add(await ReadSnapshotAsync(source, request.Count, cancellationToken));
        }

        var initialEvents = snapshots
            .SelectMany(snapshot => snapshot.Events)
            .OrderBy(logEvent => logEvent.Timestamp)
            .TakeLast(request.Count)
            .ToList();

        if (!request.Follow)
        {
            return new TailResult(initialEvents, Empty());
        }

        var channel = Channel.CreateUnbounded<LogEventRecord>();

        foreach (var snapshot in snapshots)
        {
            _ = Task.Run(() => FollowAsync(snapshot, channel.Writer, cancellationToken), CancellationToken.None);
        }

        return new TailResult(initialEvents, channel.Reader.ReadAllAsync(cancellationToken));
    }

    private async Task<TailSnapshot> ReadSnapshotAsync(ResolvedLogSource source, int count, CancellationToken cancellationToken)
    {
        var filePath = source.Path;

        if (!File.Exists(filePath))
        {
            return new TailSnapshot(source, [], 0);
        }

        var queue = new Queue<LogEventRecord>(count);

        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, leaveOpen: true);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!parser.TryParse(line, source.DisplayName, filePath, out var logEvent))
            {
                continue;
            }

            if (queue.Count == count)
            {
                queue.Dequeue();
            }

            queue.Enqueue(logEvent);
        }

        return new TailSnapshot(source, queue.ToList(), stream.Length);
    }

    private async Task FollowAsync(TailSnapshot snapshot, ChannelWriter<LogEventRecord> writer, CancellationToken cancellationToken)
    {
        var currentPath = snapshot.Source.Path;
        var position = snapshot.Position;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (snapshot.Source.Kind == LogSourceKind.RollingDirectory)
            {
                var latest = SourceResolver.GetLatestFile(snapshot.Source.RollingDirectoryPath!);
                if (!string.IsNullOrWhiteSpace(latest) && !PathsEqual(currentPath, latest))
                {
                    currentPath = latest;
                    position = 0;
                }
            }

            position = await DrainAsync(snapshot.Source, currentPath, position, writer, cancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }

    private async Task<long> DrainAsync(
        ResolvedLogSource source,
        string currentPath,
        long position,
        ChannelWriter<LogEventRecord> writer,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(currentPath))
        {
            return position;
        }

        await using var stream = new FileStream(currentPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        if (stream.Length < position)
        {
            position = 0;
        }

        stream.Seek(position, SeekOrigin.Begin);
        using var reader = new StreamReader(stream, leaveOpen: true);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!parser.TryParse(line, source.DisplayName, currentPath, out var logEvent))
            {
                continue;
            }

            await writer.WriteAsync(logEvent, cancellationToken);
        }

        return stream.Length;
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(left, right, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static async IAsyncEnumerable<LogEventRecord> Empty([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    private sealed record TailSnapshot(ResolvedLogSource Source, IReadOnlyList<LogEventRecord> Events, long Position);
}
