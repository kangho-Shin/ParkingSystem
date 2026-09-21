using Parking.Contracts;

namespace Parking.EdgeService;

public interface IKioskExitCoordinator
{
    Task<FieldEventResponse?> CompleteAsync(
        long kioskDeviceId,
        Guid eventId,
        CancellationToken cancellationToken);
}
