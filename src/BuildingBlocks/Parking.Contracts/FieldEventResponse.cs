using Newtonsoft.Json;

namespace Parking.Contracts;

public sealed record FieldEventResponse(
    [property: JsonConverter(typeof(GuidNJsonConverter))]
    Guid EventId,
    bool Accepted,
    long? ParkingSessionId,
    string ResultCode,
    string DisplayMessage,
    bool OpenBarrier);
