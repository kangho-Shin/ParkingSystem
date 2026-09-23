using Newtonsoft.Json;

namespace Parking.Contracts;

public sealed record FieldEventRequest(
    [property: JsonConverter(typeof(GuidNJsonConverter))]
    Guid EventId,
    long SiteId,
    long LaneId,
    long DeviceId,
    string CarNumber,
    DateTimeOffset InDateTime,
    int Groupnum = 1,
    string EventType = ParkingEventType.Entry,
    string? InImage = null,
    int CarType = 1,
    bool IsManual = false);
