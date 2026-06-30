using Lathe.Core.Domain;
using Lathe.Core.Features.Sources;
using Lathe.Core.Features.Tailing;

namespace Lathe.Web.Features.Session;

public sealed class LiveLatheSessionController(
    SourceResolver sourceResolver,
    TailService tailService,
    LatheSessionState session,
    string workingDirectory,
    int count,
    CancellationToken applicationCancellationToken)
    : ILatheSessionController
{
    public bool CanAddSources => true;

    public async Task<IReadOnlyList<string>> AddInputsAsync(
        IReadOnlyList<string> files,
        IReadOnlyList<string> globs,
        IReadOnlyList<string> rollingDirectories,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = new ResolveSourcesRequest(workingDirectory, files, globs, rollingDirectories);
            var resolved = sourceResolver.Resolve(request);
            var addedSources = session.TryAddSources(resolved);

            if (addedSources.Count == 0)
            {
                return ["All resolved sources are already loaded."];
            }

            var result = await tailService.StartAsync(
                new TailRequest(addedSources, count, true),
                applicationCancellationToken);

            session.AppendRange(result.InitialEvents);
            _ = PumpLiveEventsAsync(result.LiveEvents, applicationCancellationToken);

            return [];
        }
        catch (SourceResolutionException ex)
        {
            return ex.Errors;
        }
    }

    private async Task PumpLiveEventsAsync(IAsyncEnumerable<LogEventRecord> liveEvents, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var logEvent in liveEvents.WithCancellation(cancellationToken))
            {
                session.Append(logEvent);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
