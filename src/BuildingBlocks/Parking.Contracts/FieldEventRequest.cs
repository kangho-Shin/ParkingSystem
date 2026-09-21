namespace Parking.Contracts;

public sealed record FieldEventRequest(
    Guid EventId,
    long SiteId,
    long LaneId,
    long DeviceId,
    string CarNumber,
    DateTimeOffset InDateTime,
    int Groupnum = 1,
    string EventType = ParkingEventType.Entry,
    string? InImage = null);
