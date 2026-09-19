using Parking.Contracts;

namespace Parking.Central.Data;

public interface IParkingExitRepository
{
    Task<OpenParkingSessionResponse?> FindOpenAsync(long siteId, string carNumber, CancellationToken cancellationToken);
    Task<OpenParkingSessionResponse?> FindOpenByIdAsync(long parkingSessionId, CancellationToken cancellationToken);
    Task MarkSettledAsync(long parkingSessionId, DateTimeOffset settledAt, CancellationToken cancellationToken);
    Task<FieldEventResponse> SaveExitAsync(
        ExitEventRequest request,
        bool exitAllowed,
        CancellationToken cancellationToken);
}
