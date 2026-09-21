namespace Parking.TerminalAgent;

public sealed class RestartLimiter
{
    private readonly int _maxRestarts;
    private readonly TimeSpan _window;
    private readonly Queue<DateTimeOffset> _attempts = new();

    public RestartLimiter(int maxRestarts, TimeSpan window)
    {
        if (maxRestarts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxRestarts));
        if (window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(window));

        _maxRestarts = maxRestarts;
        _window = window;
    }

    public bool TryAcquire(DateTimeOffset now, out DateTimeOffset retryAfter)
    {
        while (_attempts.Count > 0 && now - _attempts.Peek() >= _window)
            _attempts.Dequeue();

        if (_attempts.Count >= _maxRestarts)
        {
            retryAfter = _attempts.Peek() + _window;
            return false;
        }

        _attempts.Enqueue(now);
        retryAfter = now;
        return true;
    }

    public void Reset() => _attempts.Clear();
}
