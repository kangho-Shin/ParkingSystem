namespace Parking.EdgeService;

public sealed class KioskConnectionRegistry
{
    private readonly object _lock = new();
    private readonly Dictionary<long, string> _byDevice = new();
    private readonly Dictionary<string, long> _byConnection = new();

    public void Register(long deviceId, string connectionId)
    {
        lock (_lock)
        {
            if (_byDevice.TryGetValue(deviceId, out string? oldConnection))
                _byConnection.Remove(oldConnection);
            if (_byConnection.TryGetValue(connectionId, out long oldDevice))
                _byDevice.Remove(oldDevice);
            _byDevice[deviceId] = connectionId;
            _byConnection[connectionId] = deviceId;
        }
    }

    public void Unregister(string connectionId)
    {
        lock (_lock)
        {
            if (!_byConnection.Remove(connectionId, out long deviceId)) return;
            if (_byDevice.TryGetValue(deviceId, out string? current) && current == connectionId)
                _byDevice.Remove(deviceId);
        }
    }

    public bool TryGetConnection(long deviceId, out string connectionId)
    {
        lock (_lock) return _byDevice.TryGetValue(deviceId, out connectionId!);
    }
}
