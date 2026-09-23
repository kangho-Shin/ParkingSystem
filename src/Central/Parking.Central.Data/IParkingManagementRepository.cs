using Parking.Contracts;

namespace Parking.Central.Data;

public interface IParkingManagementRepository
{
    Task<PagedParkingResult<ParkingManagementItem>> SearchEntriesAsync(
        ParkingManagementQuery query,
        CancellationToken cancellationToken);

    Task<PagedParkingResult<ParkingManagementItem>> SearchExitsAsync(
        ParkingManagementQuery query,
        CancellationToken cancellationToken);

    Task<bool> CorrectCarNumberAsync(
        long siteId,
        ParkingSessionType sessionType,
        long parkingSessionId,
        string carNumber,
        CancellationToken cancellationToken);
}
