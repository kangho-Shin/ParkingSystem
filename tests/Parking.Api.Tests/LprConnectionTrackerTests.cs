using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class LprConnectionTrackerTests
{
    [Fact]
    public void LPR장비는_4대제한없이_각각추적한다()
    {
        LprConnectionTracker tracker = new();
        IDisposable[] connections = Enumerable.Range(1, 6)
            .Select(number => tracker.Track($"192.168.0.{number}:29200"))
            .ToArray();

        Assert.Equal(6, tracker.ConnectionCount);

        connections[2].Dispose();
        Assert.Equal(5, tracker.ConnectionCount);

        connections[2].Dispose();
        Assert.Equal(5, tracker.ConnectionCount);

        foreach (IDisposable connection in connections)
            connection.Dispose();
        Assert.Equal(0, tracker.ConnectionCount);
    }
}
