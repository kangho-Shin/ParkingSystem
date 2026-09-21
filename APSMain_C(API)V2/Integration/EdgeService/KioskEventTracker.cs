using System.Collections.Concurrent;

namespace APSMain.Integration.EdgeService;

public sealed class KioskEventTracker
{
    private readonly ConcurrentDictionary<Guid, EventState> _states = new();
    public bool TryBegin(Guid eventId) => _states.TryAdd(eventId, EventState.Processing);
    public void MarkFailed(Guid eventId) => _states.TryRemove(eventId, out _);
    public void MarkCompleted(Guid eventId) => _states[eventId] = EventState.Completed;
    private enum EventState { Processing, Completed }
}
