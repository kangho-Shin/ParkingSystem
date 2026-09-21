using Microsoft.AspNetCore.SignalR;
using Parking.Contracts;

namespace Parking.EdgeService;

public interface IKioskNotifier
{
    Task SendAsync(string connectionId, KioskExitNotification notification, CancellationToken cancellationToken);
}

public sealed class SignalRKioskNotifier : IKioskNotifier
{
    private readonly IHubContext<KioskHub> _hub;
    public SignalRKioskNotifier(IHubContext<KioskHub> hub) { _hub = hub; }
    public Task SendAsync(string connectionId, KioskExitNotification notification, CancellationToken cancellationToken) =>
        _hub.Clients.Client(connectionId).SendAsync(
            "ExitVehicleDetected", notification, cancellationToken);
}

public sealed class KioskNotificationService
{
    private readonly KioskConnectionRegistry _connections;
    private readonly KioskPendingRepository _pending;
    private readonly IKioskNotifier _notifier;

    public KioskNotificationService(
        KioskConnectionRegistry connections,
        KioskPendingRepository pending,
        IKioskNotifier notifier)
    {
        _connections = connections;
        _pending = pending;
        _notifier = notifier;
    }

    public async Task<bool> EnqueueAndNotifyAsync(
        long kioskDeviceId, LprRecognition recognition, CancellationToken cancellationToken)
    {
        await _pending.EnqueueAsync(kioskDeviceId, recognition, cancellationToken);
        return await TryNotifyAsync(kioskDeviceId, recognition, cancellationToken);
    }

    public async Task DeliverPendingAsync(long kioskDeviceId, CancellationToken cancellationToken)
    {
        foreach (PendingKioskEvent pending in await _pending.GetPendingAsync(kioskDeviceId, cancellationToken))
            if (!await TryNotifyAsync(kioskDeviceId, pending.Recognition, cancellationToken)) return;
    }

    private async Task<bool> TryNotifyAsync(
        long kioskDeviceId, LprRecognition recognition, CancellationToken cancellationToken)
    {
        if (!_connections.TryGetConnection(kioskDeviceId, out string connectionId)) return false;
        KioskExitNotification notification = new(
            recognition.EventId, recognition.SiteId, recognition.Groupnum,
            recognition.DeviceId, kioskDeviceId, recognition.CarNumber,
            recognition.RecognizedAt, recognition.ImageFileName);
        try
        {
            await _notifier.SendAsync(connectionId, notification, cancellationToken);
            await _pending.MarkNotifiedAsync(recognition.EventId, cancellationToken);
            return true;
        }
        catch when (!cancellationToken.IsCancellationRequested) { return false; }
    }
}
