using FluentAssertions;
using Lathe.Core.Domain;
using Lathe.Web.Features.Session;

namespace Lathe.Tests;

public sealed class LogPauseStateTests
{
    [Fact]
    public void GetVisibleEvents_Should_ReturnFrozenSnapshot_WhenPaused()
    {
        var pauseState = new LogPauseState();
        var firstEvent = CreateEvent("first");
        var secondEvent = CreateEvent("second");

        pauseState.Pause([firstEvent]);

        pauseState.GetVisibleEvents([firstEvent, secondEvent]).Should().ContainSingle().Which.Should().Be(firstEvent);
    }

    [Fact]
    public void GetWaitingEventCount_Should_CountEventsAddedAfterPause()
    {
        var pauseState = new LogPauseState();
        var firstEvent = CreateEvent("first");
        var secondEvent = CreateEvent("second");
        var thirdEvent = CreateEvent("third");

        pauseState.Pause([firstEvent]);

        pauseState.GetWaitingEventCount([firstEvent, secondEvent, thirdEvent]).Should().Be(2);
    }

    [Fact]
    public void Resume_Should_ReturnCurrentEvents()
    {
        var pauseState = new LogPauseState();
        var firstEvent = CreateEvent("first");
        var secondEvent = CreateEvent("second");

        pauseState.Pause([firstEvent]);
        pauseState.Resume();

        pauseState.GetVisibleEvents([firstEvent, secondEvent]).Should().Equal(firstEvent, secondEvent);
        pauseState.GetWaitingEventCount([firstEvent, secondEvent]).Should().Be(0);
    }

    private static LogEventRecord CreateEvent(string message) =>
        new(DateTimeOffset.Parse("2026-06-30T00:00:00Z"), "Information", message, null, "app", "app.json", "{}");
}
