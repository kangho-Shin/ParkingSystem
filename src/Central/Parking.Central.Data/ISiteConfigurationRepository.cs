using Parking.Contracts;

namespace Parking.Central.Data;

public interface ISiteConfigurationRepository
{
    Task<SiteConfiguration?> GetAsync(long siteId, CancellationToken cancellationToken);
    Task SaveSiteAsync(ParkingSite site, CancellationToken cancellationToken);
    Task SaveLaneAsync(ParkingLane lane, CancellationToken cancellationToken);
    Task SaveDeviceAsync(ParkingDevice device, CancellationToken cancellationToken);
    Task SaveDeviceLinkAsync(ParkingDeviceLink link, CancellationToken cancellationToken);
    Task<bool> ValidateSiteKeyAsync(long siteId, string siteKey, CancellationToken cancellationToken);
    Task<VersionedSiteConfiguration?> GetVersionedAsync(long siteId, CancellationToken cancellationToken);
    Task<bool> SaveVersionedAsync(VersionedSiteConfiguration value, CancellationToken cancellationToken);
    Task TouchVersionAsync(long siteId, CancellationToken cancellationToken);
}
