using Parking.Contracts;

namespace Parking.EdgeService;

public interface IKioskExitCoordinator
{
    Task<FieldEventResponse?> CompleteAsync(
        long kioskDeviceId,
        Guid eventId,
        CancellationToken cancellationToken);

    Task DisplayAsync(
        long kioskDeviceId,
        long siteId,
        int groupnum,
        string carNumber,
        string displayMessage,
        int displaySeconds,
        CancellationToken cancellationToken);

    Task<FieldEventResponse?> CompleteManualAsync(
        long kioskDeviceId,
        long siteId,
        int groupnum,
        string carNumber,
        DateTimeOffset exitAt,
        CancellationToken cancellationToken);
}
