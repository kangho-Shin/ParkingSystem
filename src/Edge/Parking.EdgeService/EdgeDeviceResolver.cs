using Parking.Contracts;

namespace Parking.EdgeService;

public sealed class EdgeDeviceResolver : IEdgeDeviceResolver
{
    private readonly LocalConfigurationService _configuration;

    public EdgeDeviceResolver(LocalConfigurationService configuration)
    {
        _configuration = configuration;
    }

    public async Task<ParkingDevice> ResolveAsync(
        EdgeDeviceIdentity identity,
        CancellationToken cancellationToken)
    {
        SiteConfiguration? configuration = await _configuration.GetAsync(cancellationToken);
        if (configuration is null)
            throw new DeviceIdentityException("CONFIG_NOT_READY");
        return Resolve(configuration, identity);
    }

    public static ParkingDevice Resolve(
        SiteConfiguration configuration,
        EdgeDeviceIdentity identity)
    {
        if (identity.Sitenum != configuration.Site.SiteId)
            throw new DeviceIdentityException("INVALID_SITE");

        HashSet<long> laneIds = configuration.Lanes
            .Where(lane => lane.Enabled && lane.GroupNumber == identity.Groupnum)
            .Select(lane => lane.LaneId)
            .ToHashSet();
        List<ParkingDevice> matches = configuration.Devices
            .Where(device =>
                device.Enabled &&
                device.SiteId == identity.Sitenum &&
                device.DeviceNumber == identity.Devicenum &&
                string.Equals(device.DeviceType, identity.DeviceType, StringComparison.OrdinalIgnoreCase) &&
                device.LaneId is long laneId &&
                laneIds.Contains(laneId))
            .ToList();

        return matches.Count switch
        {
            1 => matches[0],
            > 1 => throw new DeviceIdentityException("DEVICE_AMBIGUOUS"),
            _ => throw new DeviceIdentityException("DEVICE_NOT_FOUND")
        };
    }
}
