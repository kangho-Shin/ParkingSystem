using Parking.Contracts;

namespace Parking.Central.Data;

public interface ISiteConfigurationRepository
{
    Task<SiteConfiguration?> GetAsync(long siteId, CancellationToken cancellationToken);
    Task SaveSiteAsync(ParkingSite site, CancellationToken cancellationToken);
    Task SaveLaneAsync(ParkingLane lane, CancellationToken cancellationToken);
    Task SaveDeviceAsync(ParkingDevice device, CancellationToken cancellationToken);
}
