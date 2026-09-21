using Microsoft.AspNetCore.SignalR;

namespace Parking.EdgeService;

public sealed class KioskHub : Hub
{
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

    public async Task Register(long sitenum, int groupnum, int devicenum)
    {
        if (sitenum <= 0 || groupnum <= 0 || devicenum <= 0)
            throw new HubException("INVALID_DEVICE");
        try
        {
            Parking.Contracts.ParkingDevice kiosk = await _resolver.ResolveAsync(
                new EdgeDeviceIdentity(sitenum, groupnum, devicenum, "KIOSK"),
                Context.ConnectionAborted);
            _connections.Register(kiosk.DeviceId, Context.ConnectionId);
            await _notifications.DeliverPendingAsync(kiosk.DeviceId, Context.ConnectionAborted);
        }
        catch (DeviceIdentityException ex)
        {
            throw new HubException(ex.Code);
        }
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _connections.Unregister(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
