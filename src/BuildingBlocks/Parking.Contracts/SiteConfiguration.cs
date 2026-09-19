namespace Parking.Contracts;

public sealed record ParkingSite(
    long SiteId,
    string SiteName,
    bool Enabled);

public sealed record ParkingLane(
    long LaneId,
    long SiteId,
    int GroupNumber,
    string LaneName,
    string Direction,
    bool Enabled);

public sealed record ParkingDevice(
    long DeviceId,
    long SiteId,
    long? LaneId,
    int DeviceNumber,
    string DeviceType,
    string DeviceName,
    string? IpAddress,
    bool Enabled,
    int? Port = null);

public sealed record SiteConfiguration(
    ParkingSite Site,
    IReadOnlyList<ParkingLane> Lanes,
    IReadOnlyList<ParkingDevice> Devices);
