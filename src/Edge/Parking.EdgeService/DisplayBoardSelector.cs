using Parking.Contracts;

namespace Parking.EdgeService;

public static class DisplayBoardSelector
{
    public static ParkingDevice? Find(
        SiteConfiguration configuration,
        long sourceDeviceId) =>
        DeviceLinkSelector.FindTargets(configuration, sourceDeviceId, "LDM", "LDM")
            .FirstOrDefault();
}
