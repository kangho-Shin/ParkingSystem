using System.Collections.Concurrent;

namespace Parking.EdgeService;

public sealed class LprConnectionTracker
{
    private readonly ConcurrentDictionary<long, string> _connections = new();
    private long _connectionId;

    public int ConnectionCount => _connections.Count;

    public IDisposable Track(string remoteEndpoint)
    {
        long connectionId = Interlocked.Increment(ref _connectionId);
        _connections[connectionId] = remoteEndpoint;
        return new Registration(_connections, connectionId);
    }

    private sealed class Registration : IDisposable
    {
        private ConcurrentDictionary<long, string>? _connections;
        private readonly long _connectionId;

        public Registration(
            ConcurrentDictionary<long, string> connections,
            long connectionId)
        {
            _connections = connections;
            _connectionId = connectionId;
        }

        public void Dispose() =>
            Interlocked.Exchange(ref _connections, null)?
                .TryRemove(_connectionId, out _);
    }
}
