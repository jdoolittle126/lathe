using FluentAssertions;
using Lathe.Core.Domain;
using Lathe.Core.Features.Tailing;
using Lathe.Core.Infrastructure.Logs;

namespace Lathe.Tests;

public sealed class TailServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "lathe-tests", Guid.NewGuid().ToString("n"));

    [Fact]
    public async Task StartAsync_Should_MergeSourcesByTimestampAndKeepTheLatestWindow()
    {
        Directory.CreateDirectory(_root);

        var alpha = WriteLog(
            "alpha.json",
            "{\"@t\":\"2026-04-14T10:00:00Z\",\"@m\":\"alpha-1\"}",
            "{\"@t\":\"2026-04-14T10:02:00Z\",\"@m\":\"alpha-2\"}");

        var beta = WriteLog(
            "beta.json",
            "{\"@t\":\"2026-04-14T10:01:00Z\",\"@m\":\"beta-1\"}",
            "{\"@t\":\"2026-04-14T10:03:00Z\",\"@m\":\"beta-2\"}");

        var service = new TailService(new JsonLogEventParser());
        var result = await service.StartAsync(
            new TailRequest(
                [
                    new ResolvedLogSource("alpha", alpha, LogSourceKind.File),
                    new ResolvedLogSource("beta", beta, LogSourceKind.File),
                ],
                3,
                false),
            CancellationToken.None);

        result.InitialEvents.Select(logEvent => logEvent.Message)
            .Should()
            .ContainInOrder("beta-1", "alpha-2", "beta-2");
    }

    [Fact]
    public async Task StartAsync_Should_IgnoreInvalidLines_When_FileContainsNoise()
    {
        Directory.CreateDirectory(_root);

        var file = WriteLog(
            "mixed.json",
            "not json at all",
            "{\"@t\":\"2026-04-14T10:00:00Z\",\"@m\":\"still here\"}");

        var service = new TailService(new JsonLogEventParser());
        var result = await service.StartAsync(
            new TailRequest([new ResolvedLogSource("mixed", file, LogSourceKind.File)], 10, false),
            CancellationToken.None);

        result.InitialEvents.Should().ContainSingle();
        result.InitialEvents[0].Message.Should().Be("still here");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string WriteLog(string relativePath, params string[] lines)
    {
        var fullPath = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllLines(fullPath, lines);
        return fullPath;
    }
}
