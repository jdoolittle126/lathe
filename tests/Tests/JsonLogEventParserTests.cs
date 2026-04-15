using FluentAssertions;
using Lathe.Core.Infrastructure.Logs;

namespace Lathe.Tests;

public sealed class JsonLogEventParserTests
{
    private readonly JsonLogEventParser _parser = new();

    [Fact]
    public void TryParse_Should_CaptureStructuredProperties_When_LogContainsNamedValues()
    {
        const string line = "{\"@t\":\"2026-04-14T10:00:00Z\",\"@l\":\"Information\",\"@m\":\"hello\",\"MachineName\":\"web-01\",\"ElapsedMs\":42,\"Payload\":{\"orderId\":4821}}";

        var parsed = _parser.TryParse(line, "web-01", "c:/logs/web-01.json", out var logEvent);

        parsed.Should().BeTrue();
        logEvent.Properties.Should().ContainKey("MachineName").WhoseValue.Should().Be("web-01");
        logEvent.Properties.Should().ContainKey("ElapsedMs").WhoseValue.Should().Be("42");
        logEvent.Properties.Should().ContainKey("Payload").WhoseValue.Should().Be("{\"orderId\":4821}");
    }

    [Fact]
    public void TryParse_Should_RenderMessageTemplate_When_RenderedMessageIsMissing()
    {
        const string line = "{\"@t\":\"2026-04-14T10:00:00Z\",\"@l\":\"Information\",\"@mt\":\"Telemetry event {TelemetryEventName} {@TelemetryProperties} {@TelemetryMetrics}.\",\"TelemetryEventName\":\"RequestCompleted\",\"TelemetryProperties\":{\"path\":\"/orders/4821\",\"tenant\":\"northwind\"},\"TelemetryMetrics\":{\"durationMs\":42.5}}";

        var parsed = _parser.TryParse(line, "web-01", "c:/logs/web-01.json", out var logEvent);

        parsed.Should().BeTrue();
        logEvent.Message.Should().Be("Telemetry event RequestCompleted {\"path\":\"/orders/4821\",\"tenant\":\"northwind\"} {\"durationMs\":42.5}.");
    }
}
