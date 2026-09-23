using Parking.Contracts;

namespace Parking.EdgeManager.Core;

public interface ICentralParkingClient
{
    Task<PagedParkingResult<ParkingManagementItem>> SearchEntriesAsync(
        ParkingManagementQuery query,
        CancellationToken cancellationToken);

    Task<PagedParkingResult<ParkingManagementItem>> SearchExitsAsync(
        ParkingManagementQuery query,
        CancellationToken cancellationToken);

    Task<ManualEntryResponse> CreateManualEntryAsync(
        ManualEntryRequest request,
        CancellationToken cancellationToken);

    Task CorrectCarNumberAsync(
        ParkingSessionType sessionType,
        long parkingSessionId,
        ManagementCarNumberRequest request,
        CancellationToken cancellationToken);
}
