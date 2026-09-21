namespace Parking.Contracts;

public sealed record OfflineKioskExitRequest(
    Guid EventId,
    long SiteId,
    int Groupnum,
    long LaneId,
    long DeviceId,
    string CarNumber,
    DateTimeOffset OutDateTime,
    string? OutImage);
