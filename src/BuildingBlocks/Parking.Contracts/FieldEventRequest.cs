namespace Parking.Contracts;

public sealed record FieldEventRequest(
    Guid EventId,
    long SiteId,
    long LaneId,
    long DeviceId,
    string CarNumber,
    DateTimeOffset OccurredAt);
