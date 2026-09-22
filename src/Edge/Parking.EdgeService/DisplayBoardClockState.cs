namespace Parking.EdgeService;

public sealed class DisplayBoardClockState
{
    private readonly object _sync = new();
    private readonly Dictionary<long, State> _states = new();

    public bool ShouldSend(long deviceId, DateTimeOffset now)
    {
        lock (_sync)
        {
            if (!_states.TryGetValue(deviceId, out State? state))
            {
                _states[deviceId] = new State(DateTimeOffset.MinValue, MinuteKey(now));
                return true;
            }
            if (now < state.SuppressUntil) return false;
            long minute = MinuteKey(now);
            if (state.LastMinute == minute) return false;
            state.LastMinute = minute;
            return true;
        }
    }

    public void Suppress(long deviceId, DateTimeOffset now, TimeSpan duration)
    {
        lock (_sync)
            _states[deviceId] = new State(now.Add(duration), -1);
    }

    public void Reset(long deviceId)
    {
        lock (_sync) _states.Remove(deviceId);
    }

    private static long MinuteKey(DateTimeOffset value) =>
        value.Year * 100000000L + value.Month * 1000000L + value.Day * 10000L +
        value.Hour * 100L + value.Minute;

    private sealed class State(DateTimeOffset suppressUntil, long lastMinute)
    {
        public DateTimeOffset SuppressUntil { get; } = suppressUntil;
        public long LastMinute { get; set; } = lastMinute;
    }
}
