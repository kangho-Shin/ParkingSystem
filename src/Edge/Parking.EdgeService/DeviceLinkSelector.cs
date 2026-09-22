using Parking.Contracts;

namespace Parking.EdgeService;

public static class DeviceLinkSelector
{
    public static IReadOnlyList<ParkingDevice> FindTargets(
        SiteConfiguration configuration,
        long sourceDeviceId,
        string linkType,
        string targetDeviceType)
    {
        IReadOnlyList<ParkingDeviceLink> links =
            configuration.DeviceLinks ?? Array.Empty<ParkingDeviceLink>();
        HashSet<long> targetIds = links
            .Where(link => link.Enabled &&
                link.SiteId == configuration.Site.SiteId &&
                link.SourceDeviceId == sourceDeviceId &&
                link.SourceDeviceId != link.TargetDeviceId &&
                string.Equals(link.LinkType, linkType, StringComparison.OrdinalIgnoreCase))
            .Select(link => link.TargetDeviceId)
            .ToHashSet();

        return configuration.Devices
            .Where(device => device.Enabled &&
                device.SiteId == configuration.Site.SiteId &&
                targetIds.Contains(device.DeviceId) &&
                string.Equals(device.DeviceType, targetDeviceType, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public static ParkingDevice? FindSource(
        SiteConfiguration configuration,
        long targetDeviceId,
        string linkType,
        string sourceDeviceType)
    {
        long? sourceId = (configuration.DeviceLinks ?? Array.Empty<ParkingDeviceLink>())
            .Where(link => link.Enabled &&
                link.SiteId == configuration.Site.SiteId &&
                link.TargetDeviceId == targetDeviceId &&
                link.SourceDeviceId != link.TargetDeviceId &&
                string.Equals(link.LinkType, linkType, StringComparison.OrdinalIgnoreCase))
            .Select(link => (long?)link.SourceDeviceId)
            .FirstOrDefault();
        return sourceId is null
            ? null
            : configuration.Devices.FirstOrDefault(device =>
                device.Enabled && device.DeviceId == sourceId &&
                string.Equals(device.DeviceType, sourceDeviceType, StringComparison.OrdinalIgnoreCase));
    }
}
