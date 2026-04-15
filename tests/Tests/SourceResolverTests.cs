using FluentAssertions;
using Lathe.Core.Features.Sources;

namespace Lathe.Tests;

public sealed class SourceResolverTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "lathe-tests", Guid.NewGuid().ToString("n"));
    private readonly SourceResolver _resolver = new();

    [Fact]
    public void Resolve_Should_ExpandGlobsAndDedupeMatchingFiles()
    {
        Directory.CreateDirectory(_root);

        var alpha = WriteLog("alpha.json");
        var beta = WriteLog("beta.json");

        var sources = _resolver.Resolve(new ResolveSourcesRequest(
            _root,
            [alpha],
            ["*.json"],
            []));

        sources.Should().HaveCount(2);
        sources.Select(source => source.Path).Should().BeEquivalentTo([alpha, beta]);
    }

    [Fact]
    public void Resolve_Should_UseTheLatestFile_When_RollingDirectoryIsProvided()
    {
        var rolling = Directory.CreateDirectory(Path.Combine(_root, "rolling")).FullName;

        var older = WriteLog(Path.Combine("rolling", "older.json"));
        var newer = WriteLog(Path.Combine("rolling", "newer.json"));

        File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddMinutes(-5));
        File.SetLastWriteTimeUtc(newer, DateTime.UtcNow);

        var sources = _resolver.Resolve(new ResolveSourcesRequest(
            _root,
            [],
            [],
            [rolling]));

        sources.Should().ContainSingle();
        sources[0].Path.Should().Be(newer);
        sources[0].RollingDirectoryPath.Should().Be(rolling);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string WriteLog(string relativePath)
    {
        var fullPath = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "{}\n");
        return fullPath;
    }
}
