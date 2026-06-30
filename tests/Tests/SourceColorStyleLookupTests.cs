using FluentAssertions;
using Lathe.Core.Domain;
using Lathe.Web.Features.Session;

namespace Lathe.Tests;

public sealed class SourceColorStyleLookupTests
{
    [Fact]
    public void GetStyle_Should_KeepRollingSourceColorStable_WhenLatestFileChanges()
    {
        var rollingDirectory = Path.Combine(Path.GetTempPath(), "lathe", "rolling");
        var firstLatestFile = Path.Combine(rollingDirectory, "2026-06-30-001.json");
        var secondLatestFile = Path.Combine(rollingDirectory, "2026-06-30-002.json");

        var firstSource = new ResolvedLogSource("rolling", firstLatestFile, LogSourceKind.RollingDirectory, rollingDirectory);
        var secondSource = new ResolvedLogSource("rolling", secondLatestFile, LogSourceKind.RollingDirectory, rollingDirectory);

        var firstLookup = SourceColorStyleLookup.Create([firstSource]);
        var secondLookup = SourceColorStyleLookup.Create([secondSource]);

        firstLookup.GetStyle(firstSource).Should().Be(secondLookup.GetStyle(secondSource));
    }

    [Fact]
    public void GetStyle_Should_UseRollingSourceColor_ForEventsFromRotatedFiles()
    {
        var rollingDirectory = Path.Combine(Path.GetTempPath(), "lathe", "rolling");
        var initialFile = Path.Combine(rollingDirectory, "2026-06-30-001.json");
        var rotatedFile = Path.Combine(rollingDirectory, "2026-06-30-002.json");
        var source = new ResolvedLogSource("rolling", initialFile, LogSourceKind.RollingDirectory, rollingDirectory);
        var logEvent = new LogEventRecord(
            DateTimeOffset.UtcNow,
            "Information",
            "Rotated",
            null,
            "rolling",
            rotatedFile,
            "{}");

        var lookup = SourceColorStyleLookup.Create([source]);

        lookup.GetStyle(logEvent).Should().Be(lookup.GetStyle(source));
    }

    [Fact]
    public void GetStyle_Should_DifferentiateFilesWithTheSameNameInDifferentDirectories()
    {
        var firstFile = Path.Combine(Path.GetTempPath(), "lathe", "api", "app.json");
        var secondFile = Path.Combine(Path.GetTempPath(), "lathe", "web", "app.json");
        var firstSource = new ResolvedLogSource("app.json", firstFile, LogSourceKind.File);
        var secondSource = new ResolvedLogSource("app.json", secondFile, LogSourceKind.File);

        var lookup = SourceColorStyleLookup.Create([firstSource, secondSource]);

        lookup.GetStyle(firstSource).Should().NotBe(lookup.GetStyle(secondSource));
    }
}
