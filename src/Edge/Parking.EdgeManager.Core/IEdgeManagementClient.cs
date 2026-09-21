using Parking.Contracts;

namespace Parking.EdgeManager.Core;

public interface IEdgeManagementClient
{
    Task<EdgeSetupResponse?> GetSetupAsync(CancellationToken cancellationToken);
    Task<EdgeSetupResponse> SaveSetupAsync(
        EdgeSetupRequest request,
        CancellationToken cancellationToken);
    Task<EdgeServiceStatus> GetStatusAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<EdgeEntryItem>> GetEntriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<EdgeActivityItem>> GetActivitiesAsync(CancellationToken cancellationToken);
    Task<SiteConfiguration?> GetConfigurationAsync(CancellationToken cancellationToken);
    Task SaveSiteAsync(ParkingSite value, CancellationToken cancellationToken);
    Task SaveLaneAsync(ParkingLane value, CancellationToken cancellationToken);
    Task DeleteLaneAsync(long laneId, CancellationToken cancellationToken);
    Task SaveDeviceAsync(ParkingDevice value, CancellationToken cancellationToken);
    Task DeleteDeviceAsync(long deviceId, CancellationToken cancellationToken);
    Task SaveDeviceLinkAsync(ParkingDeviceLink value, CancellationToken cancellationToken);
    Task DeleteDeviceLinkAsync(long sourceDeviceId, long targetDeviceId, string linkType, CancellationToken cancellationToken);
    Task SaveOperationVariableAsync(ParkingOperationVariable value, CancellationToken cancellationToken);
    Task DeleteOperationVariableAsync(int groupnum, string commandType, CancellationToken cancellationToken);
    Task<byte[]?> GetImageAsync(string? fileName, CancellationToken cancellationToken);
}
