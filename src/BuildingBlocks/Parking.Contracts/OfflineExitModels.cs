using Newtonsoft.Json;

namespace Parking.Contracts;

public sealed record OfflineKioskExitRequest(
    [property: JsonConverter(typeof(GuidNJsonConverter))]
    Guid EventId,
    long SiteId,
    int Groupnum,
    long LaneId,
    long DeviceId,
    string CarNumber,
    DateTimeOffset OutDateTime,
    string? OutImage);
