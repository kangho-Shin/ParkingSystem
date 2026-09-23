using Microsoft.AspNetCore.SignalR;

namespace Parking.EdgeService;

public sealed class KioskHub : Hub
{
    private const string PendingKioskDeviceIdKey = "PendingKioskDeviceId";
    private readonly KioskConnectionRegistry _connections;
    private readonly KioskNotificationService _notifications;
    private readonly IEdgeDeviceResolver _resolver;

    public KioskHub(
        KioskConnectionRegistry connections,
        KioskNotificationService notifications,
        IEdgeDeviceResolver resolver)
    {
        _connections = connections;
        _notifications = notifications;
        _resolver = resolver;
    }

    public async Task<long> Register(long sitenum, int groupnum, int devicenum)
    {
        if (sitenum <= 0 || groupnum <= 0 || devicenum <= 0)
            throw new HubException("INVALID_DEVICE");
        try
        {
            Parking.Contracts.ParkingDevice kiosk = await _resolver.ResolveAsync(
                new EdgeDeviceIdentity(sitenum, groupnum, devicenum, "KIOSK"),
                Context.ConnectionAborted);
            Context.Items[PendingKioskDeviceIdKey] = kiosk.DeviceId;
            return kiosk.DeviceId;
        }
        catch (DeviceIdentityException ex)
        {
            throw new HubException(ex.Code);
        }
    }

    public async Task DeliverPending()
    {
        if (!Context.Items.TryGetValue(PendingKioskDeviceIdKey, out object? value) ||
            value is not long kioskDeviceId ||
            kioskDeviceId <= 0)
            throw new HubException("DEVICE_NOT_REGISTERED");
        _connections.Register(kioskDeviceId, Context.ConnectionId);
        await _notifications.DeliverPendingAsync(kioskDeviceId, Context.ConnectionAborted);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _connections.Unregister(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
