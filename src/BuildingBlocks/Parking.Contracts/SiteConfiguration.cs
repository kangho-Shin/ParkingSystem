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

public sealed record ParkingDeviceLink(
    long SiteId,
    long SourceDeviceId,
    long TargetDeviceId,
    string LinkType,
    bool Enabled);

public sealed record ParkingOperationVariable(
    int Groupnum,
    string CommandType,
    string? Value);

public sealed record SiteConfiguration(
    ParkingSite Site,
    IReadOnlyList<ParkingLane> Lanes,
    IReadOnlyList<ParkingDevice> Devices,
    IReadOnlyList<ParkingDeviceLink>? DeviceLinks = null,
    IReadOnlyList<ParkingOperationVariable>? OperationVariables = null);
