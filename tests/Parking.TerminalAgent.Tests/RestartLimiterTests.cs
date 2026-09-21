using Parking.TerminalAgent;

namespace Parking.TerminalAgent.Tests;

public sealed class RestartLimiterTests
{
    [Fact]
    public void AllowsOnlyConfiguredRestartsInsideWindow()
    {
        RestartLimiter limiter = new(maxRestarts: 3, TimeSpan.FromMinutes(5));
        DateTimeOffset now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

        Assert.True(limiter.TryAcquire(now, out _));
        Assert.True(limiter.TryAcquire(now.AddMinutes(1), out _));
        Assert.True(limiter.TryAcquire(now.AddMinutes(2), out _));
        Assert.False(limiter.TryAcquire(now.AddMinutes(3), out DateTimeOffset retryAfter));
        Assert.Equal(now.AddMinutes(5), retryAfter);
    }

    [Fact]
    public void AllowsRestartAfterOldestAttemptLeavesWindow()
    {
        RestartLimiter limiter = new(maxRestarts: 2, TimeSpan.FromMinutes(5));
        DateTimeOffset now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

        Assert.True(limiter.TryAcquire(now, out _));
        Assert.True(limiter.TryAcquire(now.AddMinutes(1), out _));
        Assert.True(limiter.TryAcquire(now.AddMinutes(5), out _));
    }

    [Fact]
    public void StableRunClearsPreviousFailures()
    {
        RestartLimiter limiter = new(maxRestarts: 2, TimeSpan.FromMinutes(5));
        DateTimeOffset now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

        Assert.True(limiter.TryAcquire(now, out _));
        Assert.True(limiter.TryAcquire(now.AddMinutes(1), out _));

        limiter.Reset();

        Assert.True(limiter.TryAcquire(now.AddMinutes(2), out _));
    }
}
