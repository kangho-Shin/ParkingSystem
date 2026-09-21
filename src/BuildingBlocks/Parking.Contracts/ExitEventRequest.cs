namespace Parking.Contracts;

public sealed record ExitEventRequest(
    Guid EventId,
    long SiteId,
    long LaneId,
    long DeviceId,
    string CarNumber,
    DateTimeOffset OutDateTime,
    int Groupnum = 1,
    int CarType = 1,
    List<int>? DiscountKeys = null,
    string EventType = ParkingEventType.Exit,
    string? OutImage = null);
