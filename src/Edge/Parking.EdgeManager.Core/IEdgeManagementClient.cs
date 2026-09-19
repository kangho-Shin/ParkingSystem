using Parking.Contracts;

namespace Parking.EdgeManager.Core;

public interface IEdgeManagementClient
{
    Task<EdgeServiceStatus> GetStatusAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<EdgeEntryItem>> GetEntriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<EdgeActivityItem>> GetActivitiesAsync(CancellationToken cancellationToken);
    Task<SiteConfiguration?> GetConfigurationAsync(CancellationToken cancellationToken);
    Task<byte[]?> GetImageAsync(string? fileName, CancellationToken cancellationToken);
}
