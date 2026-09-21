using APSMain.Integration.EdgeService;

namespace APSMain.EdgeIntegration.Tests;

public sealed class KioskEventTrackerTests
{
    [Fact]
    public void Failed_event_can_begin_again_but_completed_event_cannot()
    {
        KioskEventTracker tracker = new();
        Guid eventId = Guid.NewGuid();

        Assert.True(tracker.TryBegin(eventId));
        Assert.False(tracker.TryBegin(eventId));
        tracker.MarkFailed(eventId);
        Assert.True(tracker.TryBegin(eventId));
        tracker.MarkCompleted(eventId);
        Assert.False(tracker.TryBegin(eventId));
    }
}
