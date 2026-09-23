using Parking.Contracts;

namespace Parking.EdgeManager.Core;

public sealed record VehicleManagementButtonState(
    bool SearchEnabled,
    bool CloseEnabled,
    bool EditEnabled);

public static class VehicleManagementFormPolicy
{
    public static IReadOnlyList<ParkingDevice> EntryDevices(SiteConfiguration configuration)
    {
        HashSet<long> laneIds = configuration.Lanes
            .Where(x => x.Enabled && string.Equals(x.Direction, "ENTRY", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.LaneId)
            .ToHashSet();

        return configuration.Devices
            .Where(x => x.Enabled && x.LaneId.HasValue && laneIds.Contains(x.LaneId.Value) &&
                string.Equals(x.DeviceType, "LPR", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.DeviceNumber)
            .ToArray();
    }

    public static VehicleManagementButtonState RequestStarted() =>
        new(false, true, false);

    public static VehicleManagementButtonState AfterRequestFailed() =>
        new(true, true, true);

    public static VehicleManagementButtonState RequestCompleted() =>
        new(true, true, true);

    public static (DateTimeOffset From, DateTimeOffset To) DefaultExitRange(DateTimeOffset now) =>
        (new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset), now);

    public static bool ManagementEnabled(CentralConnectionResponse? connection) =>
        connection is not null && connection.SiteId > 0 &&
        Uri.TryCreate(connection.CentralServerUrl, UriKind.Absolute, out _) &&
        !string.IsNullOrWhiteSpace(connection.SiteAuthKey);
}
