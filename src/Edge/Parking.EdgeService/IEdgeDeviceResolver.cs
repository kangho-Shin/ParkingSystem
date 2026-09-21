using Parking.Contracts;

namespace Parking.EdgeService;

public interface IEdgeDeviceResolver
{
    Task<ParkingDevice> ResolveAsync(
        EdgeDeviceIdentity identity,
        CancellationToken cancellationToken);
}
